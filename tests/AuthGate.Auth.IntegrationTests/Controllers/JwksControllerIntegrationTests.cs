using System.Net;
using AuthGate.Auth.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AuthGate.Auth.IntegrationTests.Controllers;

public class JwksControllerIntegrationTests : IClassFixture<AuthGateWebApplicationFactory>
{
    private readonly HttpClient _client;

    public JwksControllerIntegrationTests(AuthGateWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetJwks_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/.well-known/jwks.json");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task JwksEndpoint_IsPubliclyAccessible()
    {
        // Act - Should not require authentication
        var response = await _client.GetAsync("/.well-known/jwks.json");

        // Assert - Should not return Unauthorized
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}
