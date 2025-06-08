using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.SQS;
using Mailing.Lambda.Api.Api;
using Mailing.Lambda.Api.Authentication;
using Mailing.Lambda.Core.Bus;
using Mailing.Lambda.Core.Mailing.Endpoints;
using Mailing.Lambda.Core.Mailing.Repository;
using Mailing.Lambda.Core.Utils;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
         .SetBasePath(Directory.GetCurrentDirectory())
         .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
         .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
         .AddEnvironmentVariables();

// Add services to the container.
// builder.Services.AddControllers();
builder.Services.AddSingleton<IDynamoDBContext, DynamoDBContext>(p => new DynamoDBContext(new AmazonDynamoDBClient()));
builder.Services.AddSingleton<IAmazonSQS>(s => new AmazonSQSClient(new AmazonSQSConfig
{
  ServiceURL = EnvitonmentUtils.GetEnvironmentVariable("SQS_SERVICE_URL", "https://sqs.us-east-1.amazonaws.com")
}));

// builder.Services.AddAuthorization();
// builder.Services.AddAuthentication(ApiKeySchemeOptions.Scheme)
// .AddScheme<ApiKeySchemeOptions, ApiKeySchemeHandler>(
//         ApiKeySchemeOptions.Scheme, options =>
//         {
//           options.HeaderName = "X-API-KEY";
//         });

// Add AWS Lambda support. When application is run in Lambda Kestrel is swapped out as the web server with Amazon.Lambda.AspNetCoreServer. This
// package will act as the webserver translating request and responses between the Lambda event source and ASP.NET Core.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

builder.Services.AddScoped<IBusService>(b =>
{
  var sqsClient = b.GetRequiredService<IAmazonSQS>();
  var logger = b.GetRequiredService<ILogger<SQSQueueService>>();
  // Puedes pasar aquí cualquier configuración necesaria, por ejemplo el nombre de la cola desde config
  return new SQSQueueService(sqsClient, logger, EnvitonmentUtils.GetEnvironmentVariable("PRINCIPAL_QUEUE_URL", ""));
});

builder.Services.AddScoped<IMailingClientRepository, MailingClientRepository>();
builder.Services.AddScoped<SendEmailEndpoint>();

var app = builder.Build();

// app.UseAuthentication();
// app.UseAuthorization();
// // app.MapControllers();

app.UsePathBase(new PathString("/api"));

app.MapGet("/", () => "Welcome to running ASP.NET Core Minimal API on AWS Lambda");

app.MapEmailApiV1();
app.Run();

