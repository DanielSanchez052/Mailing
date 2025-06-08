using System;

namespace Mailing.Lambda.Core.Mailing.Models;

public class SendMailMessage
{
  public MailingRequest request { get; set; } = new MailingRequest();
  public string clientId { get; set; } = default!; // Default value to avoid null reference issues
}
