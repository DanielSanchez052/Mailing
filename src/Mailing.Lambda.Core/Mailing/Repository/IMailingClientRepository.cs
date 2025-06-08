using System;
using Mailing.Lambda.Core.Mailing.Models;

namespace Mailing.Lambda.Core.Mailing.Repository;

public interface IMailingClientRepository
{
  Task<ClientModel?> GetClientByApiKey(string apiKey);
  Task<ClientModel>? GetClientByid(string clientId);
}
