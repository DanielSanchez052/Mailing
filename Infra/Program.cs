using System;
using Amazon.CDK;
using Infra;

string service = GetEnvironmentVariable("SERVICE", "test-app");
string stage = GetEnvironmentVariable("STAGE", "dev");
string account = GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT");
string region = GetEnvironmentVariable("CDK_DEFAULT_REGION");

var app = new App();
new AppStack(app, $"{service}-{stage}--app", new AppStackProps(stage, service)
{
    Description = $"{service} {stage} application stack",
    Env = new Amazon.CDK.Environment
    {
        Account = account,
        Region = region,
    }
});
app.Synth();

static string GetEnvironmentVariable(string name, string defaultValue = "")
{
    try
    {
        var value = System.Environment.GetEnvironmentVariable(name);
        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }
    catch
    {
        return defaultValue;
    }
}