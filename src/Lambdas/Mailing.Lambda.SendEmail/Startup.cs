using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.SQS;
using Mailing.Lambda.Core.Bus;
using Mailing.Lambda.Core.Mailing.Endpoints;
using Mailing.Lambda.Core.Mailing.Repository;
using Mailing.Lambda.Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mailing.Lambda.SendEmail;

[Amazon.Lambda.Annotations.LambdaStartup]
public class Startup
{
    /// <summary>
    /// Services for Lambda functions can be registered in the services dependency injection container in this method. 
    ///
    /// The services can be injected into the Lambda function through the containing type's constructor or as a
    /// parameter in the Lambda function using the FromService attribute. Services injected for the constructor have
    /// the lifetime of the Lambda compute container. Services injected as parameters are created within the scope
    /// of the function invocation.
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDynamoDBContext, DynamoDBContext>(p => new DynamoDBContext(new AmazonDynamoDBClient()));
        services.AddSingleton<IAmazonSQS>(s => new AmazonSQSClient(new AmazonSQSConfig
        {
            ServiceURL = EnvitonmentUtils.GetEnvironmentVariable("SQS_SERVICE_URL", "https://sqs.us-east-1.amazonaws.com")
        }));

        services.AddScoped<IBusService>(b =>
        {
            var sqsClient = b.GetRequiredService<IAmazonSQS>();
            var logger = b.GetRequiredService<ILogger<SQSQueueService>>();
            // Puedes pasar aquí cualquier configuración necesaria, por ejemplo el nombre de la cola desde config
            return new SQSQueueService(sqsClient, logger, EnvitonmentUtils.GetEnvironmentVariable("PRINCIPAL_QUEUE_URL", ""));
        });

        services.AddScoped<IMailingClientRepository, MailingClientRepository>();
        services.AddScoped<SendEmailEndpoint>();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddLambdaLogger(new LambdaLoggerOptions
            {
                IncludeCategory = true,
                IncludeLogLevel = true,
                IncludeEventId = true,
                IncludeException = true,
                IncludeNewline = true,
                IncludeScopes = true
            });
        });


        //// Add AWS Systems Manager as a potential provider for the configuration. This is 
        //// available with the Amazon.Extensions.Configuration.SystemsManager NuGet package.
        //builder.AddSystemsManager("/app/settings");

        // var configuration = builder.Build();
        // services.AddSingleton<IConfiguration>(configuration);
    }
}
