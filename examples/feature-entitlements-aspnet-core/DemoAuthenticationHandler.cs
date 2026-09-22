using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace KilnBook;

// Fixed identities make this local sample reproducible. Use your real identity provider in an application.
public sealed class DemoAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var (customer, canCreate) = Request.Headers.Authorization.ToString() switch
        {
            "Bearer basic-owner" => ("clay-room", true),
            "Bearer studio-owner" => ("river-studio", true),
            "Bearer studio-viewer" => ("river-studio", false),
            "Bearer visitor-owner" => ("visitor", true),
            _ => ((string?)null, false)
        };
        if (customer is null)
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Request.Headers.Authorization.ToString()),
            new("customer_key", customer)
        };
        if (canCreate)
            claims.Add(new("permission", "reservations:create"));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
