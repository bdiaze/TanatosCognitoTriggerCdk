using Amazon.Lambda.CognitoEvents;
using Amazon.Lambda.Core;
using Amazon.SecretsManager;
using Amazon.SimpleSystemsManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
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
			#endregion
		});

		IHost app = builder.Build();

		serviceProvider = app.Services;
	}

	public async Task<CognitoPostConfirmationEvent> FunctionHandler(CognitoPostConfirmationEvent cognitoEvent, ILambdaContext context) {
		Stopwatch stopwatch = Stopwatch.StartNew();

		string userName = cognitoEvent.UserName;
		string triggerSource = cognitoEvent.TriggerSource;

		LambdaLogger.Log(
			$"[Function] - [FunctionHandler] - " +
			$"Se inicia trigger de Cognito con parámetros - TriggerSource: {triggerSource} - Sub {userName}");

		if (triggerSource == "PostConfirmation_ConfirmSignUp") {
			SuscripcionDao suscripcionDao = serviceProvider.GetRequiredService<SuscripcionDao>();
			await suscripcionDao.ActivarSuscripcionGratuita(userName);
		}

		LambdaLogger.Log(
			$"[Function] - [FunctionHandler] - [{stopwatch.ElapsedMilliseconds} ms] - " +
			$"Se terminan de ejecutar el trigger de Cognito.");

		return cognitoEvent;
	}
}
