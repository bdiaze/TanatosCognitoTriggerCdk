using Amazon.Lambda.CognitoEvents;
using Amazon.Lambda.Core;
using Amazon.SecretsManager;
using Amazon.SimpleSystemsManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Text.Json;
using TanatosCognitoTrigger.Entities;
using TanatosCognitoTrigger.Helpers;
using TanatosCognitoTrigger.Repositories;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace TanatosCognitoTrigger;

public class Function {
	private readonly IServiceProvider serviceProvider;
	private readonly string[] CUSTOM_EMAIL_EVENTS = [
		"CustomEmailSender_SignUp",
		"CustomEmailSender_ForgotPassword",
		"CustomEmailSender_ResendCode",
		"CustomEmailSender_UpdateUserAttribute",
		"CustomEmailSender_VerifyUserAttribute",
		"CustomEmailSender_Authentication",
		"CustomEmailSender_AdminCreateUser",
		"CustomEmailSender_AccountTakeOverNotification"
	];

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
			services.AddSingleton<PerfilDao>();
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
		} else if (CUSTOM_EMAIL_EVENTS.Contains(triggerSource)) {
			CognitoCustomEmailSenderEvent cognitoEvento = JsonSerializer.Deserialize<CognitoCustomEmailSenderEvent>(jsonCognitoEvento.GetRawText()) ?? throw new InvalidOperationException("CognitoCustomEmailSenderEvent no definido");

			string? nombre = cognitoEvento.Request.UserAttributes.TryGetValue("given_name", out string? given_name) ? (given_name != null && !given_name.StartsWith("cognito:") ? given_name : null) : null;
			string correoElectronico = cognitoEvento.Request.UserAttributes["email"];
			string codigoEncriptado = cognitoEvento.Request.Code;

			PerfilDao profileDao = serviceProvider.GetRequiredService<PerfilDao>();
			await profileDao.EnviarCodigoVerificacion(nombre, correoElectronico, codigoEncriptado, triggerSource switch {
				"CustomEmailSender_SignUp" => TipoCodigoVerificacion.SignUp,
				"CustomEmailSender_ForgotPassword" => throw new InvalidOperationException("TriggerSource inválido"), // TipoCodigoVerificacion.ForgotPassword,
				"CustomEmailSender_ResendCode" => TipoCodigoVerificacion.ResendCode,
				"CustomEmailSender_UpdateUserAttribute" => TipoCodigoVerificacion.UpdateUserAttribute,
				"CustomEmailSender_VerifyUserAttribute" => TipoCodigoVerificacion.VerifyUserAttribute,
				"CustomEmailSender_Authentication" => TipoCodigoVerificacion.Authentication,
				"CustomEmailSender_AdminCreateUser" => TipoCodigoVerificacion.AdminCreateUser,
				"CustomEmailSender_AccountTakeOverNotification" => TipoCodigoVerificacion.AccountTakeOverNotification,
				_ => throw new InvalidOperationException("TriggerSource inválido")
			});
		} else {
			throw new InvalidOperationException("TriggerSource inválido");
		}

		LambdaLogger.Log(
			$"[Function] - [FunctionHandler] - [{stopwatch.ElapsedMilliseconds} ms] - " +
			$"Se terminan de ejecutar el trigger de Cognito.");

		return jsonCognitoEvento;
	}
}
