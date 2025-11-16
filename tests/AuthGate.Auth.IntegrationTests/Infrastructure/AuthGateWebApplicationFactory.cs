using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AuthGate.Auth.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for AuthGate integration tests
/// Configures test environment without DB for basic controller tests
/// </summary>
public class AuthGateWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
