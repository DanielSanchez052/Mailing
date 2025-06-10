using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Mailing.Lambda.Core.Mailing.Models;
using Mailing.Lambda.Core.Types;
using Amazon.Lambda.Core;
using Mailing.Lambda.Core.Bus;
using Amazon.SQS.Model;

namespace Mailing.Lambda.Core.Utils;


[JsonSerializable(typeof(APIGatewayCustomAuthorizerRequest))]
[JsonSerializable(typeof(APIGatewayCustomAuthorizerResponse))]
[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(MailingRequest))]
[JsonSerializable(typeof(ClientModel))]
[JsonSerializable(typeof(Response<string>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(ILambdaContext))]
[JsonSerializable(typeof(QueueMessageResponse))]
[JsonSerializable(typeof(SendMailMessage))]
[JsonSerializable(typeof(SendMessageRequest))]
public partial class AppSerializerContext : JsonSerializerContext
{

}
