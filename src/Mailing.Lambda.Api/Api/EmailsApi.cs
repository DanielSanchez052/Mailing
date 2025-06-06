using Mailing.Lambda.Core.Mailing;
using Mailing.Lambda.Core.Types;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Mailing.Lambda.Api.Api;

public static class EmailsApi
{
    public static IEndpointRouteBuilder MapEmailApiV1(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("v1/mails");

        api.MapPost("/", SendEmailAsync);

        return app;
    }


    public static async Task<Results<Ok<Response<string>>, BadRequest<Response<string>>>> SendEmailAsync
      (
          [FromServices] MailingService? service,
          [FromServices] ILogger logger,
          [FromBody] MailingRequest request
      )
    {
        if (request == null)
            return TypedResults.BadRequest(Response<string>.InvalidRequestError());

        if (service == null)
        {
            logger.LogError("MailingService not available");
            return TypedResults.BadRequest(Response<string>.InternalServerError());
        }

        try
        {
            var result = await service.SendEmailAsync(request);
            return TypedResults.Ok(Response<string>.Success(result));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email");
            return TypedResults.BadRequest(Response<string>.InternalServerError());
        }
    }
}
