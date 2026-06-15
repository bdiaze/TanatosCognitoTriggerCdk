using Amazon.Lambda.CognitoEvents;
using Amazon.Lambda.Core;
using Amazon.SecretsManager;
using Amazon.SimpleSystemsManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Text.Json;
using TanatosCognitoTrigger.Helpers;
using TanatosCognitoTrigger.Repositories;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace TanatosCognitoTrigger;

public class Function {
	private readonly IServiceProvider serviceProvider;

	public Function() {
		IHostBuilder builder = Host.CreateDefaultBuilder();
		builder.ConfigureServices((context, services) => {
			#region Singleton AWS Services
			services.AddSingleton<IAmazonSimpleSystemsManagement, AmazonSimpleSystemsManagementClient>();
			services.AddSingleton<IAmazonSecretsManager, AmazonSecretsManagerClient>();
			#endregion

			#region Singleton Helpers
			services.AddSingleton<VariableEntornoHelper>();
			services.AddSingleton<ParameterStoreHelper>();
			services.AddSingleton<SecretManagerHelper>();
			services.AddSingleton<ClientCredentialsHelper>();
			#endregion

			#region Singleton Repositories
			services.AddSingleton<SuscripcionDao>();
			services.AddSingleton<ProfileDao>();
			#endregion
		});
		IHost app = builder.Build();
		serviceProvider = app.Services;
	}

	public async Task<JsonElement> FunctionHandler(JsonElement jsonCognitoEvento, ILambdaContext context) {
		Stopwatch stopwatch = Stopwatch.StartNew();

		string triggerSource = jsonCognitoEvento.GetProperty("triggerSource").GetString() ?? throw new InvalidOperationException("Trigger Source no definido");
		
		LambdaLogger.Log(
			$"[Function] - [FunctionHandler] - " +
			$"Se inicia trigger de Cognito con parámetros - TriggerSource: {triggerSource}");

		if (triggerSource == "PostConfirmation_ConfirmSignUp") {
			CognitoPostConfirmationEvent cognitoEvento = JsonSerializer.Deserialize<CognitoPostConfirmationEvent>(jsonCognitoEvento.GetRawText()) ?? throw new InvalidOperationException("CognitoPostConfirmationEvent no definido");

			SuscripcionDao suscripcionDao = serviceProvider.GetRequiredService<SuscripcionDao>();
			await suscripcionDao.ActivarSuscripcionGratuita(cognitoEvento.UserName);
		} else if (triggerSource == "CustomEmailSender_SignUp") {
			CognitoCustomEmailSenderEvent cognitoEvento = JsonSerializer.Deserialize<CognitoCustomEmailSenderEvent>(jsonCognitoEvento.GetRawText()) ?? throw new InvalidOperationException("CognitoCustomEmailSenderEvent no definido");

			string? nombre = cognitoEvento.Request.UserAttributes.TryGetValue("name", out string? value) ? value : null;
			string correoElectronico = cognitoEvento.Request.UserAttributes["email"];
			string codigoEncriptado = cognitoEvento.Request.Code;

			ProfileDao profileDao = serviceProvider.GetRequiredService<ProfileDao>();
			await profileDao.EnviarCodigoVerificacion(nombre, correoElectronico, codigoEncriptado);
		}

		LambdaLogger.Log(
			$"[Function] - [FunctionHandler] - [{stopwatch.ElapsedMilliseconds} ms] - " +
			$"Se terminan de ejecutar el trigger de Cognito.");

		return jsonCognitoEvento;
	}
}
