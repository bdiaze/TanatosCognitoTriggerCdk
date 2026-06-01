using Amazon.Lambda.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TanatosCognitoTrigger.Entities;
using TanatosCognitoTrigger.Helpers;

namespace TanatosCognitoTrigger.Repositories {
	internal class SuscripcionDao(VariableEntornoHelper variableEntorno, ParameterStoreHelper parameterStore, SecretManagerHelper secretManagerHelper, ClientCredentialsHelper clientCredentialsHelper) {
		private readonly string[] _cognitoTriggerScopes = [
			"api/suscripciones.read.all",
			"api/suscripciones.write.all",
			"api/sistema.read.public",
		];

		private readonly Lazy<Task<ApiConfig>> _config = new(() => InicializarApiConfig(variableEntorno, parameterStore, secretManagerHelper));

		private static async Task<ApiConfig> InicializarApiConfig(VariableEntornoHelper variableEntorno, ParameterStoreHelper parameterStore, SecretManagerHelper secretManagerHelper) {
			Stopwatch stopwatch = Stopwatch.StartNew();

			Task<string> taskParametro = parameterStore.ObtenerParametro(variableEntorno.Obtener("ARN_PARAMETER_TANATOS_API_URL"));
			Task<string> taskSecreto = secretManagerHelper.ObtenerSecreto(variableEntorno.Obtener("ARN_SECRET_TANATOS_API"));

			taskParametro.ContinueWith(t => LambdaLogger.Log($"[SuscripcionDao] - [InicializarApiConfig] - [{stopwatch.ElapsedMilliseconds} ms] - TaskParametro"));
			taskSecreto.ContinueWith(t => LambdaLogger.Log($"[SuscripcionDao] - [InicializarApiConfig] - [{stopwatch.ElapsedMilliseconds} ms] - TaskSecreto"));

			await Task.WhenAll(taskParametro, taskSecreto);
			string baseUrl = taskParametro.Result;
			Dictionary<string, string> secretos = JsonSerializer.Deserialize<Dictionary<string, string>>(taskSecreto.Result)!;

			return new ApiConfig { 
				BaseUrl = baseUrl,
				CognitoBaseUrl = secretos["CognitoBaseUrl"],
				CognitoTriggerClientId = secretos["CognitoTriggerUserPoolClientId"],
				CognitoTriggerClientSecret = secretos["CognitoTriggerUserPoolClientSecret"],
			};
		}

		public async Task ActivarSuscripcionGratuita(string sub) {
			Stopwatch stopwatch = Stopwatch.StartNew();

			ApiConfig config = await _config.Value;

			LambdaLogger.Log(
				$"[SuscripcionDao] - [ActivarSuscripcionGratuita] - [{stopwatch.ElapsedMilliseconds} ms] - " +
				$"Se ejecuta await ApiConfig.");

			using HttpClient client = new(new RetryHandler(new HttpClientHandler()));
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await clientCredentialsHelper.ObtenerAccessToken(
				config.CognitoBaseUrl,
				config.CognitoTriggerClientId, 
				config.CognitoTriggerClientSecret, 
				_cognitoTriggerScopes
			));

			LambdaLogger.Log(
				$"[SuscripcionDao] - [ActivarSuscripcionGratuita] - [{stopwatch.ElapsedMilliseconds} ms] - " +
				$"Se ejecuta ObtenerAccessToken(...)");

			EntSuscripcionActivarSuscripcionGratuita entrada = new() { 
				Sub = sub
			};

			HttpResponseMessage response = await client.PostAsync(config.BaseUrl + "/Suscripcion/ActivarSuscripcionGratuita", new StringContent(JsonSerializer.Serialize(entrada), Encoding.UTF8, "application/json"));
			if (!response.IsSuccessStatusCode) {
				throw new HttpRequestException(
					$"Ocurrió un error al activar la suscripción gratuita. StatusCode: {response.StatusCode} - Content: {await response.Content.ReadAsStringAsync()}",
					inner: null,
					statusCode: response.StatusCode
				);
			}

			LambdaLogger.Log(
				$"[SuscripcionDao] - [ActivarSuscripcionGratuita] - [{stopwatch.ElapsedMilliseconds} ms] - " +
				$"Se ejecuta PostAsync(...)");
		}
	}

	internal class ApiConfig {
		public required string BaseUrl { get; init; }
		public required string CognitoBaseUrl { get; init; }
		public required string CognitoTriggerClientId { get; init; }
		public required string CognitoTriggerClientSecret { get; init; }
	}
}
