using System;
using Amazon.DynamoDBv2.DataModel;

namespace Mailing.Lambda.Core.Mailing.Models;

[DynamoDBTable("client")]
public class ClientModel
{

    [DynamoDBHashKey]
    public string ClientId { get; set; } = null!;
    [DynamoDBProperty]
    public string ApiKey { get; set; } = null!;
    [DynamoDBProperty]
    public string ClientName { get; set; } = null!;
    [DynamoDBProperty]
    public string Status { get; set; } = null!;
    [DynamoDBProperty]
    public string CreatedAt { get; set; } = null!;
    [DynamoDBProperty]
    public string? LastUpdatedAt { get; set; } = null!;
    [DynamoDBProperty]
    public string DefaultSenderEmail { get; set; } = null!;
    [DynamoDBProperty]
    public List<ProviderModel> Providers { get; set; } = new();
}


public class ProviderModel
{
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = null!;
    public int Priority { get; set; }
    public Dictionary<string, string> Credentials { get; set; } = new();
    public Dictionary<string, string> Config { get; set; } = new();

}

public enum ClientStatus
{
    Inactive = 0,
    Active = 1,
    PendingSetup = 2,
}