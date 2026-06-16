using Amazon.CDK;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.CloudWatch.Actions;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SNS.Subscriptions;
using Amazon.CDK.AWS.SSM;
using Constructs;
using System;
using System.Collections.Generic;

namespace TanatosCognitoTriggerCdk
{
    public class TanatosCognitoTriggerCdkStack : Stack {
        internal TanatosCognitoTriggerCdkStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props) {
			string appName = System.Environment.GetEnvironmentVariable("APP_NAME") ?? throw new InvalidOperationException("No se ha configurado la variable de entorno APP_NAME");

			string cognitoTriggerLambdaDirectory = System.Environment.GetEnvironmentVariable("COGNITO_TRIGGER_LAMBDA_DIRECTORY") ?? throw new InvalidOperationException("No se ha configurado la variable de entorno COGNITO_TRIGGER_LAMBDA_DIRECTORY");
			string cognitoTriggerLambdaHandler = System.Environment.GetEnvironmentVariable("COGNITO_TRIGGER_LAMBDA_HANDLER") ?? throw new InvalidOperationException("No se ha configurado la variable de entorno COGNITO_TRIGGER_LAMBDA_HANDLER");
			string cognitoTriggerLambdaMemorySize = System.Environment.GetEnvironmentVariable("COGNITO_TRIGGER_LAMBDA_MEMORY_SIZE") ?? throw new InvalidOperationException("No se ha configurado la variable de entorno COGNITO_TRIGGER_LAMBDA_MEMORY_SIZE");
			string cognitoTriggerLambdaTimeout = System.Environment.GetEnvironmentVariable("COGNITO_TRIGGER_LAMBDA_TIMEOUT") ?? throw new InvalidOperationException("No se ha configurado la variable de entorno COGNITO_TRIGGER_LAMBDA_TIMEOUT");

			string arnParameterTanatosApiUrl = System.Environment.GetEnvironmentVariable("ARN_PARAMETER_TANATOS_API_URL") ?? throw new InvalidOperationException("No se ha configurado la variable de entorno ARN_PARAMETER_TANATOS_API_URL");
			string arnSecretTanatosApi = System.Environment.GetEnvironmentVariable("ARN_SECRET_TANATOS_API") ?? throw new InvalidOperationException("No se ha configurado la variable de entorno ARN_SECRET_TANATOS_API");

			string notificationEmails = System.Environment.GetEnvironmentVariable("NOTIFICATION_EMAILS") ?? throw new InvalidOperationException("No se ha configurado la variable de entorno NOTIFICATION_EMAILS");
						
			#region Log Group y Role
			// Creación de log group lambda...
			LogGroup lambdaLogGroup = new(this, $"{appName}CognitoTriggerLogGroup", new LogGroupProps {
				LogGroupName = $"/aws/lambda/{appName}CognitoTrigger/logs",
				Retention = RetentionDays.ONE_MONTH,
				RemovalPolicy = RemovalPolicy.DESTROY
			});

			// Creación de role para la función lambda...
			Role roleLambda = new(this, $"{appName}CognitoTriggerLambdaRole", new RoleProps {
				RoleName = $"{appName}CognitoTriggerLambdaRole",
				Description = $"Role para Lambda Cognito Trigger de {appName}",
				AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
				ManagedPolicies = [
					ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaVPCAccessExecutionRole"),
					ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole"),
				],
				InlinePolicies = new Dictionary<string, PolicyDocument> {
					{
						$"{appName}CognitoTriggerLambdaPolicy",
						new PolicyDocument(new PolicyDocumentProps {
							Statements = [
								new PolicyStatement(new PolicyStatementProps{
									Sid = $"{appName}AccessToParameterStore",
									Actions = [
										"ssm:GetParameter"
									],
									Resources = [
										arnParameterTanatosApiUrl,
									],
								}),
								new PolicyStatement(new PolicyStatementProps{
									Sid = $"{appName}AccessToSecretManager",
									Actions = [
										"secretsmanager:GetSecretValue"
									],
									Resources = [
										arnSecretTanatosApi,
									],
								}),
							]
						})
					}
				}
			});
			#endregion

			#region Lambda
			// Creación de la función lambda...
			Function function = new(this, $"{appName}CognitoTriggerLambdaFunction", new FunctionProps {
				FunctionName = $"{appName}CognitoTrigger",
				Description = $"Lambda encargada de procesar eventos gatillados por Cognito de la aplicacion {appName}",
				Runtime = Runtime.DOTNET_10,
				Handler = cognitoTriggerLambdaHandler,
				Code = Code.FromAsset($"{cognitoTriggerLambdaDirectory}/publish/publish.zip"),
				Timeout = Duration.Seconds(double.Parse(cognitoTriggerLambdaTimeout)),
				MemorySize = double.Parse(cognitoTriggerLambdaMemorySize),
				Architecture = Architecture.X86_64,
				LogGroup = lambdaLogGroup,
				Environment = new Dictionary<string, string> {
					{ "APP_NAME", appName },
					{ "ARN_PARAMETER_TANATOS_API_URL", arnParameterTanatosApiUrl },
					{ "ARN_SECRET_TANATOS_API", arnSecretTanatosApi },
				},
				Role = roleLambda,
			});
			#endregion

			#region Parameter Store
			// Creación de los string parameters...
			_ = new StringParameter(this, $"{appName}StringParameterLambdaArn", new StringParameterProps {
				ParameterName = $"/{appName}/CognitoTrigger/LambdaArn",
				Description = $"ARN de la Lambda Cognito Trigger de la aplicacion {appName}",
				StringValue = function.FunctionArn,
				Tier = ParameterTier.STANDARD,
			});
			#endregion

			#region Alarm
			// Se crea SNS topic para notificaciones...
			Topic topic = new(this, $"{appName}CognitoTriggerNotificationSNSTopic", new TopicProps {
				TopicName = $"{appName}CognitoTriggerNotificationSNSTopic",
			});

			foreach (string email in notificationEmails.Split(",")) {
				topic.AddSubscription(new EmailSubscription(email));
			}

			// Se crea alarma para enviar notificación cuando llegue un elemento al DLQ...
			Alarm alarmEmail = new(this, $"{appName}CognitoTriggerQueueAlarm", new AlarmProps {
				AlarmName = $"{appName}CognitoTriggerQueueAlarm",
				AlarmDescription = $"Alarma para notificar errores en Lambda Cognito Trigger de la aplicacion {appName}",
				Metric = function.MetricErrors(),
				Threshold = 1,
				EvaluationPeriods = 1,
				ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
				TreatMissingData = TreatMissingData.NOT_BREACHING,
			});
			alarmEmail.AddAlarmAction(new SnsAction(topic));
			#endregion
		}
	}
}
