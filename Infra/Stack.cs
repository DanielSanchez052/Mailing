using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.AppConfig;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SQS;
using Constructs;

namespace Infra;

internal class AppStackProps : StackProps
{
    public AppStackProps(string stage, string service)
    {
        Stage = stage;
        Service = service;
    }

    public string Stage { get; set; }
    public string Service { get; set; }
}

public class AppStack : Stack
{
    private AppStackProps _props;
    internal AppStack(Construct scope, string id, AppStackProps? props = null) : base(scope, id, props)
    {
        _props = props ?? new AppStackProps("dev", "mailing-app");
        var alarmTopic = new Topic(this, "AlarmTopic", new TopicProps()
        {
            TopicName = $"{this.StackName}-alarm",
        });

        var queue = new Queue(this, $"{id}--primary-queue", new QueueProps
        {
            QueueName = $"{this.StackName}-primary-queue",
            VisibilityTimeout = Duration.Seconds(30),
            RetentionPeriod = Duration.Days(14),
            ReceiveMessageWaitTime = Duration.Seconds(20),
            DeliveryDelay = Duration.Seconds(0),
            DeadLetterQueue = new DeadLetterQueue
            {
                MaxReceiveCount = 5,
                Queue = new Queue(this, $"{id}--dead-letter-queue", new QueueProps
                {
                    QueueName = $"{this.StackName}-dead-letter-queue",
                    RetentionPeriod = Duration.Days(14),
                })
            },
        });

        var db = new Database(this, "mailing-db");

        var mailingApiFunction = new Lambda(this, "minimal-api-lambda",
            new LambdaProps("../src", "Lambdas/Mailing.Lambda.SendEmail", _props.Stage, _props.StackName ?? $"app-mailing-api--{_props.Stage}")
            {
                Handler = "Mailing.Lambda.SendEmail",
                AlarmTopic = alarmTopic,
                IsAot = true,
                MemorySize = 1024,
                Environment = new Dictionary<string, string>
                {
                    { "STAGE", _props.Stage },
                    { "SERVICE", _props.Service },
                    { "PRINCIPAL_QUEUE_URL", queue.QueueUrl },
                    { "REGION", props?.Env?.Region ?? "us-east-1" },
                    { "ACCOUNT", props?.Env?.Account ?? "123456789012" },
                    { "ANNOTATIONS_HANDLER", "SendEmail"}
                },
            });

        var authorizerFunction = new Lambda(this, "mailing-authorizer-lambda",
           new LambdaProps("../src", "Lambdas/Mailing.Lambda.Authorizer", _props.Stage, _props.StackName ?? $"app-mailing-api--{_props.Stage}")
           {
               Handler = "Mailing.Lambda.Authorizer",
               IsAot = true,
               MemorySize = 512,
               Environment = new Dictionary<string, string>
               {
                    { "STAGE", _props.Stage },
                    { "SERVICE", _props.Service },
                    { "REGION", props?.Env?.Region ?? "us-east-1" },
                    { "ACCOUNT", props?.Env?.Account ?? "123456789012" }
               },
           });

        var messageProcessorFunction = new Lambda(this, "message-processor-lambda",
            new LambdaProps("../src", "Lambdas/Mailing.Lambda.MessageProcessor/Mailing.Lambda.MessageProcessor", _props.Stage, _props.StackName ?? $"message-processor--{_props.Stage}")
            {
                Handler = "Mailing.Lambda.MessageProcessor::Mailing.Lambda.MessageProcessor.Function::FunctionHandler",
                AlarmTopic = alarmTopic,
                IsAot = false,
                MemorySize = 512,
                Environment = new Dictionary<string, string>
                {
                    { "STAGE", _props.Stage },
                    { "SERVICE", _props.Service },
                    { "PRINCIPAL_QUEUE_URL", queue.QueueUrl },
                    { "REGION", props?.Env?.Region ?? "us-east-1" },
                    { "ACCOUNT", props?.Env?.Account ?? "123456789012" }
                },
            });

        // Grant permissions to the Lambda function to access the database
        db.ClientTable.GrantReadData(mailingApiFunction.LambdaFn);
        db.ClientTable.GrantDescribeTable(mailingApiFunction.LambdaFn);

        db.ClientTable.GrantReadData(authorizerFunction.LambdaFn);
        db.ClientTable.GrantDescribeTable(authorizerFunction.LambdaFn);

        // Grant permissions to the Lambda function to access the SQS Queue
        queue.GrantSendMessages(mailingApiFunction.LambdaFn);


        queue.GrantConsumeMessages(messageProcessorFunction.LambdaFn);
        messageProcessorFunction.LambdaFn.AddEventSource(new Amazon.CDK.AWS.Lambda.EventSources.SqsEventSource(queue));

        var apiGw = new RestApi(this, "mailing-api-gateway", new RestApiProps
        {
            RestApiName = $"{_props.StackName}-ApiGateway",
            DeployOptions = new StageOptions()
            {
                StageName = _props.Stage,
                TracingEnabled = false,
            },
            DefaultCorsPreflightOptions = new CorsOptions()
            {
                AllowMethods = Cors.ALL_METHODS,
                AllowHeaders = "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token,X-Amz-User-Agent,X-API-KEY".Split(','),
                AllowOrigins = Cors.ALL_ORIGINS,
                MaxAge = Duration.Seconds(60)
            },
            CloudWatchRole = false,
        });

        var api = apiGw.Root.AddResource("api");
        var v1 = api.AddResource("v1");
        var mails = v1.AddResource("mails");

        mails.AddMethod("POST", new LambdaIntegration(mailingApiFunction.LambdaFn), new MethodOptions
        {
            OperationName = "SendEmail",
            AuthorizationType = AuthorizationType.CUSTOM,
            ApiKeyRequired = false,
            Authorizer = new TokenAuthorizer(this, "mailing-authorizer", new TokenAuthorizerProps
            {
                Handler = authorizerFunction.LambdaFn,
                IdentitySource = "method.request.header.ApiKey",
                AuthorizerName = "MailingAuthorizer",
                ResultsCacheTtl = Duration.Seconds(0),
            })
        });

        // Output
        _ = new CfnOutput(this, "APIGWEndpoint", new CfnOutputProps
        {
            Value = apiGw.Url,
        });
    }
}
