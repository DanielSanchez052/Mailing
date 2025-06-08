using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Mailing.Lambda.Core.Mailing.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Mailing.Lambda.Api.Authentication;


public class ApiKeySchemeOptions : AuthenticationSchemeOptions
{
  public const string Scheme = "ApiKeyScheme";

  public string HeaderName { get; set; } = HeaderNames.Authorization;
}

public class ApiKeySchemeHandler : AuthenticationHandler<ApiKeySchemeOptions>
{
  private IMailingClientRepository _repository;
  public ApiKeySchemeHandler(IMailingClientRepository repository, IOptionsMonitor<ApiKeySchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : base(options, logger, encoder)
  {
    _repository = repository;
  }

  protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    if (!Request.Headers.ContainsKey(Options.HeaderName))
    {

      return AuthenticateResult.Fail("Header Not Found.");
    }

    var headerValue = Request.Headers[Options.HeaderName];

    var client = await _repository.GetClientByApiKey(headerValue.ToString());

    if (client is null)
    {
      return AuthenticateResult.Fail("Wrong Api Key.");
    }

    var claims = new Claim[]
    {
      new Claim(ClaimTypes.NameIdentifier, $"{client.ClientId}"),
      new Claim(ClaimTypes.Name, client.ClientName)
    };

    var identiy = new ClaimsIdentity(claims, nameof(ApiKeySchemeHandler));
    var principal = new ClaimsPrincipal(identiy);
    var ticket = new AuthenticationTicket(principal, Scheme.Name);

    Context.Items["Client"] = client;

    return AuthenticateResult.Success(ticket);
  }
}
