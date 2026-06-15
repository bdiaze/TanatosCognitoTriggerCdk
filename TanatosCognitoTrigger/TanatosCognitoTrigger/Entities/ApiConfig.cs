using System;
using System.Collections.Generic;
using System.Text;

namespace TanatosCognitoTrigger.Entities {
	internal class ApiConfig {
		public required string BaseUrl { get; init; }
		public required string CognitoBaseUrl { get; init; }
		public required string CognitoTriggerClientId { get; init; }
		public required string CognitoTriggerClientSecret { get; init; }
	}
}
