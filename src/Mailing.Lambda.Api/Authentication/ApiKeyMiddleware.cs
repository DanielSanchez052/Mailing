using System.Security.Claims;
using Mailing.Lambda.Core.Mailing.Repository;

namespace Mailing.Lambda.Api.Authentication
{
  public class ApiKeyMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private readonly string _headerName;

    public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
    {
      _next = next;
      _logger = logger;
      _headerName = "X-API-KEY";
    }

    public async Task InvokeAsync(HttpContext context, IMailingClientRepository repository)
    {
      if (!context.Request.Headers.TryGetValue(_headerName, out var apiKey))
      {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("API Key header not found.");
        return;
      }

      var client = await repository.GetClientByApiKey(apiKey);
      if (client == null)
      {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Invalid API Key.");
        return;
      }

      var claims = new[]
      {
                new Claim(ClaimTypes.NameIdentifier, client.ClientId.ToString()),
                new Claim(ClaimTypes.Name, client.ClientName)
            };
      var identity = new ClaimsIdentity(claims, "ApiKey");
      var principal = new ClaimsPrincipal(identity);
      context.User = principal;
      context.Items["Client"] = client;

      await _next(context);
    }
  }
}
