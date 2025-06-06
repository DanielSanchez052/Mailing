using Mailing.Lambda.Core.Types;
using Microsoft.Extensions.Logging;

namespace Mailing.Lambda.Core.Mailing.Endpoints;

public class SendEmail
{
    private readonly ILogger<SendEmail> _logger;
    private readonly MailingService _mailingService;

    public SendEmail(ILogger<SendEmail> logger, MailingService mailingService)
    {
        _logger = logger;
        _mailingService = mailingService;
    }

    public async Task<Response<string>> ExecuteAsync(MailingRequest? request)
    {
        if (request == null) 
            return Response<string>.InvalidRequestError();        

        try
        {
            var result = await _mailingService.SendEmailAsync(request);
            return Response<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            return Response<string>.InternalServerError();
        }
    }
}
