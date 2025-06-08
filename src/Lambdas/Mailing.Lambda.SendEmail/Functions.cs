using Amazon.Lambda.Core;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Mailing.Lambda.Core.Mailing.Models;
using Mailing.Lambda.Core.Types;
using Mailing.Lambda.Core.Mailing.Endpoints;
using Amazon.Lambda.APIGatewayEvents;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Mailing.Lambda.SendEmail;

/// <summary>
/// A collection of sample Lambda functions that provide a REST api for doing simple math calculations. 
/// </summary>
public class Functions
{

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <remarks>
    /// The <see cref="ICalculatorService"/> implementation that we
    /// instantiated in <see cref="Startup"/> will be injected here.
    /// 
    /// As an alternative, a dependency could be injected into each 
    /// Lambda function handler via the [FromServices] attribute.
    /// </remarks>

    public Functions()
    {

    }

    /// <summary>
    /// Root route that provides information about the other requests that can be made.
    /// </summary>
    /// <returns>API descriptions.</returns>
    [LambdaFunction()]
    [RestApi(LambdaHttpMethod.Get, "api/v1/mails")]
    public string Default()
    {
        var docs = @"Lambda Calculator Home:
            You can make the following requests to invoke other Lambda functions perform calculator operations:
            /mails
        ";
        return docs;
    }

    /// <summary>
    /// Perform x + y
    /// </summary>
    /// <param name="x">Left hand operand of the arithmetic operation.</param>
    /// <param name="y">Right hand operand of the arithmetic operation.</param>
    /// <returns>Sum of x and y.</returns>
    [LambdaFunction()]
    [RestApi(LambdaHttpMethod.Post, "api/v1/mails")]
    public async Task<Response<string>> SendEmail(
        [FromServices] SendEmailEndpoint usecase,
        [FromBody] MailingRequest request,
        ILambdaContext context,
        APIGatewayProxyRequest proxyRequest
        )
    {
        string? clientId = proxyRequest.RequestContext.Authorizer.GetValueOrDefault("UserId")?.ToString();
        if (string.IsNullOrEmpty(clientId))
        {
            context.Logger.LogLine("Client ID is missing in the request context.");
            return Response<string>.InvalidRequestError();
        }

        var response = await usecase.ExecuteAsync(request, clientId);
        return response;
    }
}