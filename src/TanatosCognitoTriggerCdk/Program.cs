using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TanatosCognitoTriggerCdk {
    sealed class Program {
		private Program() { }
		
        public static void Main(string[] args) {
			string appName = System.Environment.GetEnvironmentVariable("APP_NAME") ?? throw new InvalidOperationException("No se ha configurado la variable de entorno APP_NAME");
			string account = System.Environment.GetEnvironmentVariable("ACCOUNT_AWS") ?? throw new InvalidOperationException("No se ha configurado la variable de entorno ACCOUNT_AWS");
			string region = System.Environment.GetEnvironmentVariable("REGION_AWS") ?? throw new InvalidOperationException("No se ha configurado la variable de entorno REGION_AWS");

			var app = new App();
            _ = new TanatosCognitoTriggerCdkStack(app, $"Cdk{appName}CognitoTrigger", new StackProps {
				Env = new Amazon.CDK.Environment {
					Account = account,
					Region = region,
				}
			});
            app.Synth();
        }
    }
}
