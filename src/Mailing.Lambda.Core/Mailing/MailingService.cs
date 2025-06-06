using Microsoft.Extensions.Logging;

namespace Mailing.Lambda.Core.Mailing;

public class MailingService
{
    private readonly ILogger<MailingService> _logger;
    public MailingService(ILogger<MailingService> logger)
    {
        _logger = logger;
    }

    public async Task<string> SendEmailAsync(MailingRequest request)
    {
        var recipientsInfo = string.Join(", ", request.Recipients.Select(r => $"{r.Email} ({r.Type})"));
        _logger.LogInformation("Sending email to: {Recipients}", recipientsInfo);
        await Task.Delay(1000);
        _logger.LogInformation("Email sent to: {Recipients}", recipientsInfo);
        return "Email sent";
    }
}
