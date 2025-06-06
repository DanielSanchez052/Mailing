using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.SNS;
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
        _props = props ?? new AppStackProps("dev", "test-app");
        var alarmTopic = new Topic(this, "AlarmTopic", new TopicProps()
        {
            TopicName = $"{this.StackName}-alarm",
        });


        var mailingApiFunction = new Lambda(this, "MinimalApiLambda", 
            new LambdaProps("src/", "Mailing.Lambda.Api", _props.Stage, _props.StackName ?? $"app-mailing-api--{_props.Stage}" )
            {
                AlarmTopic = alarmTopic,
                IsAot = false,
            });


        var apiGw = new RestApi(this, "MailingApiGateway", new RestApiProps
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
                AllowHeaders = Cors.DEFAULT_HEADERS,
                AllowOrigins = Cors.ALL_ORIGINS,
                MaxAge = Duration.Seconds(60)
            },
            CloudWatchRole = false,
        });

        var api = apiGw.Root.AddResource("api");
        var mail = api.AddResource("", new ResourceOptions
        {
            DefaultIntegration = new LambdaIntegration(mailingApiFunction.LambdaFn, new LambdaIntegrationOptions
            {
                Proxy = true
            })
        });
        mail.AddMethod("ANY");
        mail.AddProxy();

        // Output
        _ = new CfnOutput(this, "APIGWEndpoint", new CfnOutputProps
        {
            Value = apiGw.Url,
        });
    }
}
