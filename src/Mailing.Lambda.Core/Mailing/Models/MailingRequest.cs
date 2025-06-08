using System.Collections.Generic;

namespace Mailing.Lambda.Core.Mailing.Models;

public class Recipient
{
    public string Email { get; set; } = default!;
    public string Type { get; set; } = default!;
}

public enum RecipientType
{
    To,
    Cc,
    Bcc
}

public class MailingRequest
{
    public List<Recipient> Recipients { get; set; } = new();
    public string Subject { get; set; } = default!;
    public string Body { get; set; } = default!;
}
