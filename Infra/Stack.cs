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
            new LambdaProps("../src/", "Mailing.Lambda.Api", _props.Stage, _props.StackName ?? $"app-mailing-api--{_props.Stage}")
            {
                Handler = "Mailing.Lambda.Api",
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

        // Grant permissions to the Lambda function to access the SQS Queue
        queue.GrantSendMessages(mailingApiFunction.LambdaFn);

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

        var api = apiGw.Root.AddResource("api", new ResourceOptions
        {
            DefaultIntegration = new LambdaIntegration(mailingApiFunction.LambdaFn, new LambdaIntegrationOptions
            {
                Proxy = true,
            })
        });
        api.AddMethod("ANY");
        api.AddProxy();

        // Output
        _ = new CfnOutput(this, "APIGWEndpoint", new CfnOutputProps
        {
            Value = apiGw.Url,
        });
    }
}
