using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.CloudWatch.Actions;
using Constructs;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.Logs;

namespace Infra;


internal class LambdaProps : FunctionProps
{
    public LambdaProps(string basePath, string projectPath, string stage, string stackName)
    {
        BasePath = basePath;
        ProjectPath = projectPath;
        Stage = stage;
        StackName = stackName;
    }

    public Topic? AlarmTopic { get; set; }
    public bool IsAot { get; set; }
    public string? ProjectPath { get; set; }
    public string BasePath { get; set; }
    public string Stage { get; set; }
    public string StackName { get; set; }

}

internal class Lambda : Construct
{
    private LambdaProps Props { get; set; }
    public Function LambdaFn { get; set; }
    public Lambda(Construct scope, string id, LambdaProps props) 
        : base(scope, id)
    {
        Props = props;
        LambdaFn = CreateLambda(id);

        var logs = new LogGroup(this, $"{id}LogGroup", new LogGroupProps()
        {
            LogGroupName = $"/aws/lambda/{LambdaFn.FunctionName}",
            Retention = Props.Stage == "Production" ? RetentionDays.TWO_MONTHS : RetentionDays.ONE_WEEK,
            RemovalPolicy = RemovalPolicy.DESTROY,
        });

        if(Props.AlarmTopic != null)
        {
            var alarm = new Alarm(this, $"{id}Errors", new AlarmProps()
            {
                AlarmName = $"{Props.StackName}-{id}Errors",
                ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
                Metric = LambdaFn.MetricErrors(new MetricOptions()
                { Statistic = "Sum", Period = Duration.Minutes(1) }),
                Threshold = 1,
                EvaluationPeriods = 1,
                ActionsEnabled = true,
            });

            // Todo: Sns Integration to alarm action
            alarm.AddAlarmAction(new SnsAction(Props.AlarmTopic));
        }
    }

    private Function CreateLambda(string id)
    {
        var buildOptions = new BundlingOptions()
        {
            Image = Runtime.DOTNET_8.BundlingImage,
            User = "root",
            OutputType = BundlingOutput.ARCHIVED,
            Command = [
                "/bin/sh",
                "-c",
                " dotnet tool install -g Amazon.Lambda.Tools"+
                (!Props.IsAot ? " && dotnet build "+Props.ProjectPath : "" )+
                " && dotnet lambda package --project-location "+ Props.ProjectPath +" --output-package /asset-output/function.zip"
            ]
        };

        var functionProps = Props;
        Props.Runtime = Runtime.DOTNET_8;
        Props.Architecture = Architecture.X86_64;
        Props.Timeout = Duration.Seconds(30);
        Props.MemorySize = Props.MemorySize > 128 ? Props.MemorySize : 256;
        Props.Environment = Props.Environment;
        Props.Code = Code.FromAsset(Props.BasePath, new Amazon.CDK.AWS.S3.Assets.AssetOptions()
        {
            Bundling = buildOptions,
        });

        var lambdaFn = new Function(this, id, functionProps);

        return lambdaFn;
    }


}
