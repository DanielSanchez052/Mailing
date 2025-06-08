using Mailing.Lambda.Core.Mailing.Models;
using Mailing.Lambda.Core.Mailing.Endpoints;
using Mailing.Lambda.Core.Types;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Mailing.Lambda.Api.Extensions;

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
          HttpContext context,
          [FromServices] SendEmailEndpoint usecase,
          [FromBody] MailingRequest request
      )
    {
        var client = context.GetClient();
        var response = await usecase.ExecuteAsync(request, client);
        if (response.IsSucess)
            return TypedResults.Ok(response);
        else
            return TypedResults.BadRequest(response);
    }
}
