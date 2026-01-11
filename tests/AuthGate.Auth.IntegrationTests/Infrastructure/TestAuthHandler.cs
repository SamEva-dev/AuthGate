using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace AuthGate.Auth.IntegrationTests.Infrastructure;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var orgId = Request.Headers.TryGetValue("X-Test-OrgId", out var orgValues)
            ? orgValues.ToString()
            : "00000000-0000-0000-0000-000000000001";

        var userId = Request.Headers.TryGetValue("X-Test-UserId", out var userValues)
            ? userValues.ToString()
            : "00000000-0000-0000-0000-000000000010";

        var email = Request.Headers.TryGetValue("X-Test-Email", out var emailValues)
            ? emailValues.ToString()
            : "test@test.com";

        var role = Request.Headers.TryGetValue("X-Test-Role", out var roleValues)
            ? roleValues.ToString()
            : "TenantOwner";

        var permissionsHeader = Request.Headers.TryGetValue("X-Test-Permissions", out var permValues)
            ? permValues.ToString()
            : "";

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "TestUser"),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new("organization_id", orgId)
        };

        foreach (var perm in permissionsHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim("permission", perm));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
