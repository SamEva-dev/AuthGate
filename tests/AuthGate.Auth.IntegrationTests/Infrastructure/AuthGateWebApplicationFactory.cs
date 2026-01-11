using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace AuthGate.Auth.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for AuthGate integration tests
/// Configures test environment without DB for basic controller tests
/// </summary>
public class AuthGateWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _authDbName = $"AuthGate_Test_Auth_{Guid.NewGuid():N}";
    private readonly string _auditDbName = $"AuthGate_Test_Audit_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "inmemory",
                ["Database:InMemory:AuthDbName"] = _authDbName,
                ["Database:InMemory:AuditDbName"] = _auditDbName
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestScheme";
                options.DefaultChallengeScheme = "TestScheme";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", _ => { });
        });

        builder.UseEnvironment("Testing");
    }
}
