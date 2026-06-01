using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace TanatosCognitoTrigger.Helpers {
	internal class ParameterStoreHelper(IAmazonSimpleSystemsManagement client) {
		private readonly Dictionary<string, string> parametersValues = [];

		public async Task<string> ObtenerParametro(string parameterArn) {
			if (!parametersValues.TryGetValue(parameterArn, out string? value)) {
				GetParameterResponse response = await client.GetParameterAsync(new GetParameterRequest {
					Name = parameterArn
				});

				if (response == null || response.Parameter == null) {
					throw new InvalidOperationException($"No se pudo rescatar correctamente el parámetro: {parameterArn}");
				}

				value = response.Parameter.Value!;
				parametersValues[parameterArn] = value;
			}

			return value;
		}
	}
}
