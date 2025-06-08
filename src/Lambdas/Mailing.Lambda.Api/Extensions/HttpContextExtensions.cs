using System;
using Mailing.Lambda.Core.Mailing.Models;

namespace Mailing.Lambda.Api.Extensions;

public static class HttpContextExtensions
{
  public static ClientModel? GetClient(this HttpContext context)
  {
    if (context == null) throw new ArgumentNullException(nameof(context));
    if (context.Items.TryGetValue("Client", out var clientObj) && clientObj is ClientModel client)
    {
      return client;
    }
    return null;
  }
}
