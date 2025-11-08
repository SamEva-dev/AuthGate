using AuthGate.Auth.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Infrastructure.Persistence.DataSeeding;

/// <summary>
/// Seeds initial data for authentication and authorization
/// </summary>
public class AuthDbSeeder
{
    private readonly AuthDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly ILogger<AuthDbSeeder> _logger;

    public AuthDbSeeder(
        AuthDbContext context,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        ILogger<AuthDbSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Seed Roles
            await SeedRolesAsync();

            // Seed Permissions
            await SeedPermissionsAsync();

            // Seed Admin User
            await SeedAdminUserAsync();

            // Seed Demo User
            await SeedDemoUserAsync();

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[]
        {
            new { Name = "Admin", Description = "Administrator with full access", IsSystemRole = true },
            new { Name = "User", Description = "Standard user", IsSystemRole = false },
            new { Name = "Manager", Description = "Manager with elevated permissions", IsSystemRole = false }
        };

        foreach (var roleData in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleData.Name))
            {
                var role = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = roleData.Name,
                    NormalizedName = roleData.Name.ToUpperInvariant(),
                    Description = roleData.Description,
                    IsSystemRole = roleData.IsSystemRole,
                    CreatedAtUtc = DateTime.UtcNow
                };

                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Created role: {RoleName}", roleData.Name);
                }
                else
                {
                    _logger.LogError("Failed to create role {RoleName}: {Errors}",
                        roleData.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private async Task SeedPermissionsAsync()
    {
        var permissions = new[]
        {
            new { Code = "users.read", DisplayName = "Read Users", Description = "Can view user information" },
            new { Code = "users.write", DisplayName = "Write Users", Description = "Can create and update users" },
            new { Code = "users.delete", DisplayName = "Delete Users", Description = "Can delete users" },
            new { Code = "roles.read", DisplayName = "Read Roles", Description = "Can view roles" },
            new { Code = "roles.write", DisplayName = "Write Roles", Description = "Can create and update roles" },
            new { Code = "roles.delete", DisplayName = "Delete Roles", Description = "Can delete roles" },
            new { Code = "permissions.read", DisplayName = "Read Permissions", Description = "Can view permissions" },
            new { Code = "permissions.write", DisplayName = "Write Permissions", Description = "Can assign permissions" }
        };

        foreach (var permData in permissions)
        {
            if (!_context.Permissions.Any(p => p.Code == permData.Code))
            {
                var permission = new Permission
                {
                    Id = Guid.NewGuid(),
                    Code = permData.Code,
                    NormalizedCode = permData.Code.ToUpperInvariant(),
                    DisplayName = permData.DisplayName,
                    Description = permData.Description,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _context.Permissions.Add(permission);
                _logger.LogInformation("Created permission: {PermissionCode}", permData.Code);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedAdminUserAsync()
    {
        const string adminEmail = "admin@authgate.com";
        const string adminPassword = "Admin@123";

        var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator",
                IsActive = true,
                MfaEnabled = false,
                CreatedAtUtc = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                // Assign Admin role
                await _userManager.AddToRoleAsync(adminUser, "Admin");

                // Assign all permissions to Admin role
                var adminRole = await _roleManager.FindByNameAsync("Admin");
                if (adminRole != null)
                {
                    var allPermissions = _context.Permissions.ToList();
                    foreach (var permission in allPermissions)
                    {
                        if (!_context.RolePermissions.Any(rp => rp.RoleId == adminRole.Id && rp.PermissionId == permission.Id))
                        {
                            _context.RolePermissions.Add(new RolePermission
                            {
                                RoleId = adminRole.Id,
                                PermissionId = permission.Id
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Created admin user: {Email}", adminEmail);
            }
            else
            {
                _logger.LogError("Failed to create admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            _logger.LogInformation("Admin user already exists: {Email}", adminEmail);
        }
    }

    private async Task SeedDemoUserAsync()
    {
        const string demoEmail = "demo@locaguest.com";
        const string demoPassword = "demo123";

        var existingDemo = await _userManager.FindByEmailAsync(demoEmail);
        if (existingDemo == null)
        {
            var demoUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = demoEmail,
                Email = demoEmail,
                EmailConfirmed = true,
                FirstName = "Demo",
                LastName = "User",
                IsActive = true,
                MfaEnabled = false,
                CreatedAtUtc = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(demoUser, demoPassword);
            if (result.Succeeded)
            {
                // Assign User role
                await _userManager.AddToRoleAsync(demoUser, "User");
                _logger.LogInformation("Created demo user: {Email}", demoEmail);
            }
            else
            {
                _logger.LogError("Failed to create demo user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            _logger.LogInformation("Demo user already exists: {Email}", demoEmail);
        }
    }
}
