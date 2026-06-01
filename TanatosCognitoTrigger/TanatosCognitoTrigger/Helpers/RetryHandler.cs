using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace TanatosCognitoTrigger.Helpers {
	internal class RetryHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler) {
		private const int MAX_RETRIES = 5;

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
			HttpResponseMessage? response = null;
			for (int i = 0; i < MAX_RETRIES; i++) {
				response = await base.SendAsync(request, cancellationToken);

				string? errorTypeHeader;
				try {
					errorTypeHeader = response.Headers.GetValues("ErrorType").FirstOrDefault();
				} catch (Exception) {
					errorTypeHeader = null;
				}

				if (response.IsSuccessStatusCode && errorTypeHeader == null || response.StatusCode == HttpStatusCode.BadRequest) {
					return response;
				}
			}

			return response!;
		}
	}
}
