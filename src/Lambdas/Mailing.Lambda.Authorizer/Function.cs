using System.Text.Json.Serialization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Mailing.Lambda.Core.Mailing.Models;
using Mailing.Lambda.Core.Mailing.Repository;


namespace Mailing.Lambda.Authorizer;

public class Function
{
    private static async Task Main()
    {
        Func<APIGatewayCustomAuthorizerRequest, ILambdaContext, APIGatewayCustomAuthorizerResponse> handler = FunctionHandler;
        await LambdaBootstrapBuilder.Create(handler, new SourceGeneratorLambdaJsonSerializer<LambdaFunctionJsonSerializerContext>())
            .Build()
            .RunAsync();
    }

    public static APIGatewayCustomAuthorizerResponse FunctionHandler(
        APIGatewayCustomAuthorizerRequest request, ILambdaContext context)
    {
        context.Logger.LogLine($"Received request: {request}");

        string headerName = "ApiKey";
        var dbContext = new AmazonDynamoDBClient();
        var clientRepository = new MailingClientAOTRepository(dbContext);

        var token = request.AuthorizationToken ?? (
            request.Headers != null && request.Headers.TryGetValue(headerName, out var headerValue) ? headerValue : null);

        if (string.IsNullOrEmpty(token))
        {
            context.Logger.LogLine($"Missing or empty header: {headerName}");
            return UnauthorizedResponse();
        }

        var client = clientRepository.GetClientByApiKey(token).GetAwaiter().GetResult();
        if (client == null)
        {
            context.Logger.LogLine($"Unauthorized access attempt with token: {token}");
            return UnauthorizedResponse();
        }

        return AuthorizedResponse(request, client);
    }

    private static APIGatewayCustomAuthorizerResponse AuthorizedResponse(
            APIGatewayCustomAuthorizerRequest request,
            ClientModel client)
    {
        var userId = client.ClientId;
        var email = client.DefaultSenderEmail;
        var givenName = client.ClientName;

        return new APIGatewayCustomAuthorizerResponse()
        {
            PrincipalID = userId,
            PolicyDocument = new APIGatewayCustomAuthorizerPolicy()
            {
                Statement = new List<APIGatewayCustomAuthorizerPolicy.IAMPolicyStatement>
                {
                    new APIGatewayCustomAuthorizerPolicy.IAMPolicyStatement()
                    {
                        Effect = "Allow",
                        Resource = new HashSet<string> { "*" },
                        Action = new HashSet<string> { "execute-api:Invoke"}

                    }
                }
            },

            Context = new APIGatewayCustomAuthorizerContextOutput()
            {
                {"UserId", userId },
                {"Email", email },
                {"name", givenName },
            }
        };
    }

    private static APIGatewayCustomAuthorizerResponse UnauthorizedResponse() =>
           new APIGatewayCustomAuthorizerResponse()
           {
               PrincipalID = "unauthorized-user",
               PolicyDocument = new APIGatewayCustomAuthorizerPolicy()
               {
                   Statement = new List<APIGatewayCustomAuthorizerPolicy.IAMPolicyStatement> {
                    new APIGatewayCustomAuthorizerPolicy.IAMPolicyStatement()
                    {
                        Effect = "Deny"
                    }
               }
               }
           };


}

[JsonSerializable(typeof(APIGatewayCustomAuthorizerRequest))]
[JsonSerializable(typeof(APIGatewayCustomAuthorizerResponse))]
[JsonSerializable(typeof(MailingRequest))]
[JsonSerializable(typeof(ClientModel))]
public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext
{
    // By using this partial class derived from JsonSerializerContext, we can generate reflection free JSON Serializer code at compile time
    // which can deserialize our class and properties. However, we must attribute this class to tell it what types to generate serialization code for.
    // See https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-source-generation
}



