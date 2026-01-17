using System.Net;
using System.Net.Http.Json;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.IntegrationTests.Infrastructure;
using AuthGate.Auth.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuthGate.Auth.IntegrationTests.Controllers;

public class RbacInvariantsIntegrationTests : IClassFixture<AuthGateWebApplicationFactory>
{
    private readonly AuthGateWebApplicationFactory _factory;

    public RbacInvariantsIntegrationTests(AuthGateWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static HttpClient CreateClientWithHeaders(AuthGateWebApplicationFactory factory, Guid orgId, Guid userId, string role, params string[] permissions)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-OrgId", orgId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        client.DefaultRequestHeaders.Add("X-Test-Permissions", string.Join(",", permissions));
        return client;
    }

    private async Task ResetAuthDbAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await db.Database.EnsureCreatedAsync();

        db.UserRoles.RemoveRange(db.UserRoles);
        db.Users.RemoveRange(db.Users);
        db.Roles.RemoveRange(db.Roles);
        await db.SaveChangesAsync();
    }

    private async Task SeedUserAsync(Guid orgId, Guid userId, string email, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        if (!await roleManager.RoleExistsAsync(role))
        {
            var createRole = await roleManager.CreateAsync(new Role { Name = role, NormalizedName = role.ToUpperInvariant() });
            createRole.Succeeded.Should().BeTrue();
        }

        var user = new User
        {
            Id = userId,
            UserName = email,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            OrganizationId = orgId,
            IsActive = true
        };

        var createUser = await userManager.CreateAsync(user, "Test@1234");
        createUser.Succeeded.Should().BeTrue();

        var addRole = await userManager.AddToRoleAsync(user, role);
        addRole.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task AuditLogs_DeleteEndpoint_IsNotExposed()
    {
        await ResetAuthDbAsync();

        var orgId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var callerId = Guid.Parse("00000000-0000-0000-0000-000000000010");

        var client = CreateClientWithHeaders(
            _factory,
            orgId,
            callerId,
            role: "TenantOwner",
            permissions: new[] { "auditlogs.read" });

        var response = await client.DeleteAsync($"/api/auditlogs/{Guid.NewGuid()}");
        // Either 405 (MethodNotAllowed) or 403 (Forbidden) is acceptable - both indicate DELETE is not allowed
        response.StatusCode.Should().BeOneOf(HttpStatusCode.MethodNotAllowed, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeactivateUser_LastTenantOwner_IsBlocked()
    {
        await ResetAuthDbAsync();

        var orgId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var callerId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var ownerId = Guid.Parse("00000000-0000-0000-0000-000000000011");

        await SeedUserAsync(orgId, callerId, "caller@test.com", "TenantAdmin");
        await SeedUserAsync(orgId, ownerId, "owner@test.com", "TenantOwner");

        var client = CreateClientWithHeaders(
            _factory,
            orgId,
            callerId,
            role: "TenantAdmin",
            permissions: new[] { "users.deactivate" });

        var response = await client.PostAsync($"/api/users/{ownerId}/deactivate", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        payload.Should().NotBeNull();
        payload!.Values.Any(v => v.Contains("last TenantOwner", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUser_CrossTenant_IsBlocked()
    {
        await ResetAuthDbAsync();

        var org1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var org2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var callerId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var targetId = Guid.Parse("00000000-0000-0000-0000-000000000012");

        await SeedUserAsync(org1, callerId, "caller@test.com", "TenantAdmin");
        await SeedUserAsync(org2, targetId, "target@test.com", "TenantUser");

        var client = CreateClientWithHeaders(
            _factory,
            org1,
            callerId,
            role: "TenantAdmin",
            permissions: new[] { "users.write" });

        var updateRequest = new
        {
            userId = targetId,
            firstName = "Updated"
        };

        var response = await client.PutAsJsonAsync($"/api/users/{targetId}", updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        payload.Should().NotBeNull();
        payload!.Values.Any(v => v.Contains("not found", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }
}
