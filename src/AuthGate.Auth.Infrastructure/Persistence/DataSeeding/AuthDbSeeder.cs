using AuthGate.Auth.Domain.Constants;
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

            // Seed Role Permissions
            await SeedRolePermissionsAsync();

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
            new { Name = Roles.SuperAdmin, Description = "Rôle technique réservé à l'équipe plateforme - Accès global à tous les tenants", IsSystemRole = true },
            new { Name = Roles.TenantOwner, Description = "Propriétaire du compte/organisation - Gère la facturation et l'organisation", IsSystemRole = false },
            new { Name = Roles.TenantAdmin, Description = "Administrateur désigné par l'Owner - Peut gérer les entités métier et utilisateurs", IsSystemRole = false },
            new { Name = Roles.TenantManager, Description = "Gestionnaire immobilier - Accès opérationnel avancé", IsSystemRole = false },
            new { Name = Roles.TenantUser, Description = "Utilisateur standard du tenant - Assistant, agent, consultant", IsSystemRole = false },
            new { Name = Roles.ReadOnly, Description = "Accès lecture seule - Comptable externe, auditeur", IsSystemRole = false }
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
            // Tenant Management
            new { Code = Permissions.TenantSettingsRead, DisplayName = "Read Tenant Settings", Description = "Can view tenant settings and configuration" },
            new { Code = Permissions.TenantSettingsWrite, DisplayName = "Write Tenant Settings", Description = "Can modify tenant settings and configuration" },
            new { Code = Permissions.TenantDelete, DisplayName = "Delete Tenant", Description = "Can delete tenant (dangerous operation)" },
            
            // Billing & Subscription
            new { Code = Permissions.BillingRead, DisplayName = "Read Billing", Description = "Can view billing information and invoices" },
            new { Code = Permissions.BillingWrite, DisplayName = "Write Billing", Description = "Can manage subscriptions and payment methods" },
            
            // User Management
            new { Code = Permissions.UsersRead, DisplayName = "Read Users", Description = "Can view users in the tenant" },
            new { Code = Permissions.UsersWrite, DisplayName = "Write Users", Description = "Can create and update users" },
            new { Code = Permissions.UsersDelete, DisplayName = "Delete Users", Description = "Can delete users" },
            new { Code = Permissions.UsersInvite, DisplayName = "Invite Users", Description = "Can invite new users to the tenant" },
            
            // Role Management
            new { Code = Permissions.RolesRead, DisplayName = "Read Roles", Description = "Can view roles and their permissions" },
            new { Code = Permissions.RolesAssign, DisplayName = "Assign Roles", Description = "Can assign roles to users" },
            new { Code = Permissions.RolesWrite, DisplayName = "Write Roles", Description = "Can create and modify custom roles" },
            
            // Properties Management
            new { Code = Permissions.PropertiesRead, DisplayName = "Read Properties", Description = "Can view properties" },
            new { Code = Permissions.PropertiesWrite, DisplayName = "Write Properties", Description = "Can create and update properties" },
            new { Code = Permissions.PropertiesDelete, DisplayName = "Delete Properties", Description = "Can delete properties" },
            
            // Tenants (Locataires) Management
            new { Code = Permissions.TenantsRead, DisplayName = "Read Tenants", Description = "Can view tenants (locataires)" },
            new { Code = Permissions.TenantsWrite, DisplayName = "Write Tenants", Description = "Can create and update tenants" },
            new { Code = Permissions.TenantsDelete, DisplayName = "Delete Tenants", Description = "Can delete tenants" },
            
            // Contracts Management
            new { Code = Permissions.ContractsRead, DisplayName = "Read Contracts", Description = "Can view contracts" },
            new { Code = Permissions.ContractsWrite, DisplayName = "Write Contracts", Description = "Can create and update contracts" },
            new { Code = Permissions.ContractsTerminate, DisplayName = "Terminate Contracts", Description = "Can terminate contracts" },
            new { Code = Permissions.ContractsDelete, DisplayName = "Delete Contracts", Description = "Can delete contracts" },
            
            // Documents Management
            new { Code = Permissions.DocumentsRead, DisplayName = "Read Documents", Description = "Can view documents" },
            new { Code = Permissions.DocumentsUpload, DisplayName = "Upload Documents", Description = "Can upload new documents" },
            new { Code = Permissions.DocumentsGenerate, DisplayName = "Generate Documents", Description = "Can generate documents from templates" },
            new { Code = Permissions.DocumentsDelete, DisplayName = "Delete Documents", Description = "Can delete documents" },
            
            // Analytics & Reporting
            new { Code = Permissions.AnalyticsRead, DisplayName = "Read Analytics", Description = "Can view analytics and reports" },
            new { Code = Permissions.AnalyticsExport, DisplayName = "Export Analytics", Description = "Can export data and reports" },
            
            // Logs & Audit
            new { Code = Permissions.AuditLogsRead, DisplayName = "Read Audit Logs", Description = "Can view audit logs" },
            new { Code = Permissions.SystemLogsRead, DisplayName = "Read System Logs", Description = "Can view system logs (SuperAdmin only)" }
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

    private async Task SeedRolePermissionsAsync()
    {
        foreach (var roleName in Roles.All)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                _logger.LogWarning("Role {RoleName} not found for permission assignment", roleName);
                continue;
            }

            var permissionsForRole = RolePermissionsMatrix.GetPermissionsForRole(roleName);
            
            foreach (var permissionCode in permissionsForRole)
            {
                var permission = _context.Permissions.FirstOrDefault(p => p.Code == permissionCode);
                if (permission == null)
                {
                    _logger.LogWarning("Permission {PermissionCode} not found", permissionCode);
                    continue;
                }

                if (!_context.RolePermissions.Any(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id))
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id
                    });
                    _logger.LogInformation("Assigned permission {PermissionCode} to role {RoleName}", permissionCode, roleName);
                }
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
                // Assign SuperAdmin role (permissions are already assigned via SeedRolePermissionsAsync)
                await _userManager.AddToRoleAsync(adminUser, Roles.SuperAdmin);
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
                // Assign TenantOwner role to demo user
                await _userManager.AddToRoleAsync(demoUser, Roles.TenantOwner);
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
