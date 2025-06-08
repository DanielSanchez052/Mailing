using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Mailing.Lambda.Core.Mailing.Models;
using Mailing.Lambda.Core.Mailing.Repository;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Mailing.Lambda.Authorizer;

public class Function
{
    private readonly string headerName = "ApiKey";
    private readonly IMailingClientRepository clientRepository;
    public Function()
    {
        var dbContext = new DynamoDBContext(new AmazonDynamoDBClient());
        clientRepository = new MailingClientRepository(dbContext);
    }

    public APIGatewayCustomAuthorizerResponse FunctionHandler(
        APIGatewayCustomAuthorizerRequest request, ILambdaContext context)
    {
        context.Logger.LogLine($"Received request: {request}");

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

    private APIGatewayCustomAuthorizerResponse AuthorizedResponse(
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

    private APIGatewayCustomAuthorizerResponse UnauthorizedResponse() =>
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
