using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace LicitApp.Api.Tests;

/// <summary>
/// Reemplaza a JwtBearer en los tests. Autentica si el request trae el header
/// <c>X-Test-Uid</c> (emite el claim <c>user_id</c> con ese valor); si falta, devuelve
/// NoResult para que <c>[Authorize]</c> responda 401. Así se cubren los casos con/sin token.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string UidHeader = "X-Test-Uid";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UidHeader, out var uid) || string.IsNullOrWhiteSpace(uid))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new[] { new Claim("user_id", uid.ToString()) };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
