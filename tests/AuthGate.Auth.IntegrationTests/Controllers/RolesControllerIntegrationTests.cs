using System.Net;
using System.Net.Http.Json;
using AuthGate.Auth.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AuthGate.Auth.IntegrationTests.Controllers;

public class RolesControllerIntegrationTests : IClassFixture<AuthGateWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RolesControllerIntegrationTests(AuthGateWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRoles_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/api/roles");

        // Assert - Should not be NotFound
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignPermission_IsAccessible()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/roles/{roleId}/permissions/{permissionId}", null);

        // Assert - Should not be NotFound (may be BadRequest or Unauthorized, but endpoint exists)
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemovePermission_IsAccessible()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/roles/{roleId}/permissions/{permissionId}");

        // Assert - Should not be NotFound
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }
}
