using Mailing.Lambda.Core.Mailing.Models;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace Mailing.Lambda.Core.Mailing.Repository;

public class MailingClientRepository : IMailingClientRepository
{
  private readonly IDynamoDBContext _context;
  public MailingClientRepository(IDynamoDBContext context)
  {
    _context = context;
  }

  public async Task<ClientModel?> GetClientByApiKey(string apiKey)
  {
    if (string.IsNullOrEmpty(apiKey))
      throw new ArgumentException("API key must not be null or empty.", nameof(apiKey));

    var conditions = new List<ScanCondition>
    {
      new ScanCondition(nameof(ClientModel.ApiKey), ScanOperator.Equal, apiKey)
    };

    var search = _context.ScanAsync<ClientModel>(conditions);
    var results = await search.GetNextSetAsync();

    return results.FirstOrDefault();
  }

  public Task<ClientModel>? GetClientByid(string clientId)
  {
    if (string.IsNullOrEmpty(clientId))
      throw new ArgumentException("API key must not be null or empty.", nameof(clientId));

    return _context.LoadAsync<ClientModel>(clientId);
  }

}
