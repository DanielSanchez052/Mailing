using System;
using Amazon.DynamoDBv2;
using Mailing.Lambda.Core.Mailing.Models;

namespace Mailing.Lambda.Core.Mailing.Repository;

public class MailingClientAOTRepository : IMailingClientRepository
{
  private readonly IAmazonDynamoDB _context;
  private const string TableName = "client"; // Cambia esto si tu tabla tiene otro nombre

  public MailingClientAOTRepository(IAmazonDynamoDB context)
  {
    _context = context ?? throw new ArgumentNullException(nameof(context));
  }

  public async Task<ClientModel?> GetClientByApiKey(string apiKey)
  {
    if (string.IsNullOrEmpty(apiKey))
      throw new ArgumentException("API key must not be null or empty.", nameof(apiKey));

    var request = new Amazon.DynamoDBv2.Model.ScanRequest
    {
      TableName = TableName,
      ExpressionAttributeNames = new Dictionary<string, string> { { "#ApiKey", "ApiKey" } },
      ExpressionAttributeValues = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
      {
        { ":apiKey", new Amazon.DynamoDBv2.Model.AttributeValue { S = apiKey } }
      },
      FilterExpression = "#ApiKey = :apiKey",
      Limit = 1
    };
    var response = await _context.ScanAsync(request);
    var item = response.Items.FirstOrDefault();
    return item != null ? MapToClientModel(item) : null;
  }

  public Task<ClientModel> GetClientByid(string clientId)
  {
    return GetClientByidInternal(clientId);
  }

  private async Task<ClientModel> GetClientByidInternal(string clientId)
  {
    if (string.IsNullOrEmpty(clientId))
      throw new ArgumentException("API key must not be null or empty.", nameof(clientId));

    var request = new Amazon.DynamoDBv2.Model.GetItemRequest
    {
      TableName = TableName,
      Key = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
      {
        { "ClientId", new Amazon.DynamoDBv2.Model.AttributeValue { S = clientId } }
      }
    };
    var response = await _context.GetItemAsync(request);
    if (response.Item != null && response.Item.Count > 0)
      return MapToClientModel(response.Item);
    else
      return new ClientModel(); // O lanza una excepción si prefieres
  }

  private ClientModel MapToClientModel(Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> item)
  {
    return new ClientModel
    {
      ClientId = item.TryGetValue("ClientId", out var clientId) ? clientId.S ?? string.Empty : string.Empty,
      ApiKey = item.TryGetValue("ApiKey", out var apiKey) ? apiKey.S ?? string.Empty : string.Empty,
      ClientName = item.TryGetValue("ClientName", out var clientName) ? clientName.S ?? string.Empty : string.Empty,
      Status = item.TryGetValue("Status", out var status) ? status.S ?? string.Empty : string.Empty,
      CreatedAt = item.TryGetValue("CreatedAt", out var createdAt) ? createdAt.S ?? string.Empty : string.Empty,
      LastUpdatedAt = item.TryGetValue("LastUpdatedAt", out var lastUpdatedAt) ? lastUpdatedAt.S : null,
      DefaultSenderEmail = item.TryGetValue("DefaultSenderEmail", out var defaultSenderEmail) ? defaultSenderEmail.S ?? string.Empty : string.Empty,
      Providers = item.TryGetValue("Providers", out var providers) && providers.L != null
        ? providers.L.Select(p => new ProviderModel
        {
          ProviderId = int.Parse(p.M["ProviderId"].N),
          ProviderName = p.M["ProviderName"].S ?? string.Empty,
          Priority = int.Parse(p.M["Priority"].N),
          Credentials = p.M.ContainsKey("Credentials") ? p.M["Credentials"].M.ToDictionary(k => k.Key, v => v.Value.S ?? string.Empty) : new Dictionary<string, string>(),
          Config = p.M.ContainsKey("Config") ? p.M["Config"].M.ToDictionary(k => k.Key, v => v.Value.S ?? string.Empty) : new Dictionary<string, string>()
        }).ToList()
        : new List<ProviderModel>()
    };
  }
}
