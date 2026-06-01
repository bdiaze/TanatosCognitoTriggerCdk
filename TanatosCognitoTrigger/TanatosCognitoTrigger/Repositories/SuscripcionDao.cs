using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TanatosCognitoTrigger.Entities;
using TanatosCognitoTrigger.Helpers;

namespace TanatosCognitoTrigger.Repositories {
	internal class SuscripcionDao(VariableEntornoHelper variableEntorno, ParameterStoreHelper parameterStore, SecretManagerHelper secretManagerHelper, ClientCredentialsHelper clientCredentialsHelper) {
		private readonly string _baseUrl = parameterStore.ObtenerParametro(variableEntorno.Obtener("ARN_PARAMETER_TANATOS_API_URL")).Result;
		private readonly string _cognitoBaseUrl = JsonSerializer.Deserialize<Dictionary<string, string>>(secretManagerHelper.ObtenerSecreto(variableEntorno.Obtener("ARN_SECRET_TANATOS_API")).Result)!["CognitoBaseUrl"];
		private readonly string _cognitoTriggerClientId = JsonSerializer.Deserialize<Dictionary<string, string>>(secretManagerHelper.ObtenerSecreto(variableEntorno.Obtener("ARN_SECRET_TANATOS_API")).Result)!["CognitoTriggerUserPoolClientId"];
		private readonly string _cognitoTriggerClientSecret = JsonSerializer.Deserialize<Dictionary<string, string>>(secretManagerHelper.ObtenerSecreto(variableEntorno.Obtener("ARN_SECRET_TANATOS_API")).Result)!["CognitoTriggerUserPoolClientSecret"];

		private readonly string[] _cognitoTriggerScopes = [
			"api/suscripciones.read.all",
			"api/suscripciones.write.all",
			"api/sistema.read.public",
		];

		public async Task ActivarSuscripcionGratuita(string sub) {
			using HttpClient client = new(new RetryHandler(new HttpClientHandler()));
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await clientCredentialsHelper.ObtenerAccessToken(_cognitoBaseUrl, _cognitoTriggerClientId, _cognitoTriggerClientSecret, _cognitoTriggerScopes));

			EntSuscripcionActivarSuscripcionGratuita entrada = new() { 
				Sub = sub
			};

			HttpResponseMessage response = await client.PostAsync(_baseUrl + "/Suscripcion/ActivarSuscripcionGratuita", new StringContent(JsonSerializer.Serialize(entrada), Encoding.UTF8, "application/json"));
			if (!response.IsSuccessStatusCode) {
				throw new HttpRequestException(
					$"Ocurrió un error al activar la suscripción gratuita. StatusCode: {response.StatusCode} - Content: {await response.Content.ReadAsStringAsync()}",
					inner: null,
					statusCode: response.StatusCode
				);
			}
		}
	}
}
