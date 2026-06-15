using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TanatosCognitoTrigger.Entities;
using TanatosCognitoTrigger.Helpers;

namespace TanatosCognitoTrigger.Repositories {
	internal class ProfileDao(VariableEntornoHelper variableEntorno, ParameterStoreHelper parameterStore, SecretManagerHelper secretManagerHelper, ClientCredentialsHelper clientCredentialsHelper) {
		private readonly string[] _cognitoTriggerScopes = [
			"api/profile.read.all",
			"api/profile.write.all"
		];

		private readonly Lazy<Task<ApiConfig>> _config = new(() => InicializarApiConfig(variableEntorno, parameterStore, secretManagerHelper));

		private static async Task<ApiConfig> InicializarApiConfig(VariableEntornoHelper variableEntorno, ParameterStoreHelper parameterStore, SecretManagerHelper secretManagerHelper) {
			Task<string> taskParametro = parameterStore.ObtenerParametro(variableEntorno.Obtener("ARN_PARAMETER_TANATOS_API_URL"));
			Task<string> taskSecreto = secretManagerHelper.ObtenerSecreto(variableEntorno.Obtener("ARN_SECRET_TANATOS_API"));

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

		public async Task EnviarCodigoVerificacion(string? nombre, string correoElectronico, string codigoEncriptado) {
			ApiConfig config = await _config.Value;

			using HttpClient client = new(new RetryHandler(new HttpClientHandler()));
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await clientCredentialsHelper.ObtenerAccessToken(
				config.CognitoBaseUrl,
				config.CognitoTriggerClientId,
				config.CognitoTriggerClientSecret,
				_cognitoTriggerScopes
			));

			EntProfileEnviarCodigoVerificacion entrada = new() {
				Nombre = nombre,
				CorreoElectronico = correoElectronico,
				CodigoEncriptado = codigoEncriptado
			};

			HttpResponseMessage response = await client.PostAsync(config.BaseUrl + "/Profile/EnviarCodigoVerificacion", new StringContent(JsonSerializer.Serialize(entrada), Encoding.UTF8, "application/json"));
			if (!response.IsSuccessStatusCode) {
				throw new HttpRequestException(
					$"Ocurrió un error al enviar código de verificación. StatusCode: {response.StatusCode} - Content: {await response.Content.ReadAsStringAsync()}",
					inner: null,
					statusCode: response.StatusCode
				);
			}
		}
	}
}
