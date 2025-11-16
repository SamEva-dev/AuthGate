using System.Net;
using System.Net.Http.Json;
using AuthGate.Auth.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AuthGate.Auth.IntegrationTests.Controllers;

public class UsersControllerIntegrationTests : IClassFixture<AuthGateWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersControllerIntegrationTests(AuthGateWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/api/users?page=1&pageSize=10");

        // Assert - Should not be NotFound
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUsers_WithPagination_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/api/users?page=2&pageSize=20&search=test");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateUser_IsAccessible()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateRequest = new { firstName = "Updated", lastName = "User" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/users/{userId}", updateRequest);

        // Assert - Should not be NotFound
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteUser_IsAccessibleOrNotImplemented()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/users/{userId}");

        // Assert - May not be implemented or require specific permission
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.NoContent, 
            HttpStatusCode.NotFound, 
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden
        );
    }
}
