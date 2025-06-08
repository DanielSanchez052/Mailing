using Mailing.Lambda.Core.Types;
using Mailing.Lambda.Core.Mailing.Models;
using Microsoft.Extensions.Logging;
using Mailing.Lambda.Core.Mailing.Validators;
using Mailing.Lambda.Core.Bus;

namespace Mailing.Lambda.Core.Mailing.Endpoints;

public class SendEmailEndpoint
{
    private readonly ILogger<SendEmailEndpoint> _logger;
    private readonly IBusService _busService;

    public SendEmailEndpoint(ILogger<SendEmailEndpoint> logger, IBusService busService)
    {
        _logger = logger;
        _busService = busService;
    }

    public async Task<Response<string>> ExecuteAsync(MailingRequest? request, string clientId)
    {
        if (request == null)
            return Response<string>.InvalidRequestError();

        if (string.IsNullOrEmpty(clientId))
        {
            _logger.LogError("ClientId is null or empty in SendEmail::ExecuteAsync");
            return Response<string>.InvalidRequestError();
        }

        try
        {
            var errors = MailingRequestValidator.Validate(request);
            if (errors != null && errors.Count() > 0)
                return Response<string>.ValidationError("No se ha podido enviar el email", errors);

            var queueResponse = await _busService.SendMessageAsync(new SendMailMessage
            {
                request = request,
                clientId = clientId
            });

            if (!queueResponse.IsSuccess)
            {
                _logger.LogError($"Error sending email to SQS: {queueResponse.StatusCode}");
                return Response<string>.InternalServerError();
            }

            var result = "Sending email to SQS was successful. MessageId: " + queueResponse.MessageId;
            _logger.LogInformation(result);
            return Response<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            return Response<string>.InternalServerError();
        }
    }
}
