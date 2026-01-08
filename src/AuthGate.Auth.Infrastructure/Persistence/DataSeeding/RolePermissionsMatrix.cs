using AuthGate.Auth.Domain.Constants;

namespace AuthGate.Auth.Infrastructure.Persistence.DataSeeding;

/// <summary>
/// Defines the matrix of permissions assigned to each role
/// </summary>
public static class RolePermissionsMatrix
{
    /// <summary>
    /// Gets permissions for SuperAdmin role
    /// All permissions - global access
    /// </summary>
    public static readonly string[] SuperAdminPermissions = Permissions.All;

    /// <summary>
    /// Gets permissions for TenantOwner role
    /// Full access within tenant including billing and administration
    /// </summary>
    public static readonly string[] TenantOwnerPermissions = 
    {
        // Tenant Management
        Permissions.TenantSettingsRead,
        Permissions.TenantSettingsWrite,
        Permissions.TenantDelete,

        // Billing (owner only)
        Permissions.BillingRead,
        Permissions.BillingWrite,

        // User Management
        Permissions.UsersRead,
        Permissions.UsersWrite,
        Permissions.UsersDelete,
        Permissions.UsersInvite,

        // Role Management
        Permissions.RolesRead,
        Permissions.RolesAssign,
        Permissions.RolesWrite,

        // Properties
        Permissions.PropertiesRead,
        Permissions.PropertiesWrite,
        Permissions.PropertiesDelete,

        // Tenants (Locataires)
        Permissions.TenantsRead,
        Permissions.TenantsWrite,
        Permissions.TenantsDelete,

        // Contracts
        Permissions.ContractsRead,
        Permissions.ContractsWrite,
        Permissions.ContractsTerminate,
        Permissions.ContractsDelete,

        // Documents
        Permissions.DocumentsRead,
        Permissions.DocumentsWrite,
        Permissions.DocumentsUpload,
        Permissions.DocumentsGenerate,
        Permissions.DocumentsDelete,

        // Rooms
        Permissions.RoomsRead,
        Permissions.RoomsWrite,

        // Payments
        Permissions.PaymentsRead,
        Permissions.PaymentsWrite,

        // Deposits
        Permissions.DepositsRead,
        Permissions.DepositsWrite,

        // Team
        Permissions.TeamRead,
        Permissions.TeamManage,

        // Analytics
        Permissions.AnalyticsRead,
        Permissions.AnalyticsExport,

        // Audit
        Permissions.AuditLogsRead
    };

    /// <summary>
    /// Gets permissions for TenantAdmin role
    /// Administrative access but no billing
    /// </summary>
    public static readonly string[] TenantAdminPermissions = 
    {
        // Tenant Management (read only)
        Permissions.TenantSettingsRead,

        // User Management
        Permissions.UsersRead,
        Permissions.UsersWrite,
        Permissions.UsersInvite,

        // Role Management (limited)
        Permissions.RolesRead,
        Permissions.RolesAssign,

        // Properties
        Permissions.PropertiesRead,
        Permissions.PropertiesWrite,
        Permissions.PropertiesDelete,

        // Tenants (Locataires)
        Permissions.TenantsRead,
        Permissions.TenantsWrite,
        Permissions.TenantsDelete,

        // Contracts
        Permissions.ContractsRead,
        Permissions.ContractsWrite,
        Permissions.ContractsTerminate,
        Permissions.ContractsDelete,

        // Documents
        Permissions.DocumentsRead,
        Permissions.DocumentsWrite,
        Permissions.DocumentsUpload,
        Permissions.DocumentsGenerate,
        Permissions.DocumentsDelete,

        // Rooms
        Permissions.RoomsRead,
        Permissions.RoomsWrite,

        // Payments
        Permissions.PaymentsRead,
        Permissions.PaymentsWrite,

        // Deposits
        Permissions.DepositsRead,
        Permissions.DepositsWrite,

        // Team
        Permissions.TeamRead,
        Permissions.TeamManage,

        // Analytics
        Permissions.AnalyticsRead,
        Permissions.AnalyticsExport,

        // Audit
        Permissions.AuditLogsRead
    };

    /// <summary>
    /// Gets permissions for TenantManager role
    /// Operational access without administration
    /// </summary>
    public static readonly string[] TenantManagerPermissions = 
    {
        // Properties
        Permissions.PropertiesRead,
        Permissions.PropertiesWrite,

        // Tenants (Locataires)
        Permissions.TenantsRead,
        Permissions.TenantsWrite,

        // Contracts
        Permissions.ContractsRead,
        Permissions.ContractsWrite,
        Permissions.ContractsTerminate,

        // Documents
        Permissions.DocumentsRead,
        Permissions.DocumentsUpload,
        Permissions.DocumentsGenerate,

        // Rooms
        Permissions.RoomsRead,
        Permissions.RoomsWrite,

        // Payments
        Permissions.PaymentsRead,
        Permissions.PaymentsWrite,

        // Deposits
        Permissions.DepositsRead,
        Permissions.DepositsWrite,

        // Team
        Permissions.TeamRead,

        // Analytics
        Permissions.AnalyticsRead
    };

    /// <summary>
    /// Gets permissions for TenantUser role
    /// Standard user access
    /// </summary>
    public static readonly string[] TenantUserPermissions = 
    {
        // Properties (read only)
        Permissions.PropertiesRead,

        // Tenants (Locataires)
        Permissions.TenantsRead,
        Permissions.TenantsWrite,

        // Contracts (read only)
        Permissions.ContractsRead,

        // Documents
        Permissions.DocumentsRead,
        Permissions.DocumentsUpload,

        // Rooms
        Permissions.RoomsRead,

        // Payments
        Permissions.PaymentsRead,

        // Deposits
        Permissions.DepositsRead,

        // Team
        Permissions.TeamRead,

        // Analytics (read only)
        Permissions.AnalyticsRead
    };

    /// <summary>
    /// Gets permissions for ReadOnly role
    /// Read-only access to most resources
    /// </summary>
    public static readonly string[] ReadOnlyPermissions = 
    {
        // Properties
        Permissions.PropertiesRead,

        // Tenants (Locataires)
        Permissions.TenantsRead,

        // Contracts
        Permissions.ContractsRead,

        // Documents
        Permissions.DocumentsRead,

        // Rooms
        Permissions.RoomsRead,

        // Payments
        Permissions.PaymentsRead,

        // Deposits
        Permissions.DepositsRead,

        // Team
        Permissions.TeamRead,

        // Analytics
        Permissions.AnalyticsRead,
        Permissions.AnalyticsExport
    };

    public static readonly string[] OccupantPermissions =
    {
        Permissions.DocumentsRead
    };

    public static readonly string[] OccupantAdminPermissions = TenantAdminPermissions;

    public static readonly string[] OccupantOwnerPermissions = TenantOwnerPermissions;

    /// <summary>
    /// Gets all permissions for a specific role
    /// </summary>
    public static string[] GetPermissionsForRole(string roleName)
    {
        return roleName switch
        {
            Roles.SuperAdmin => SuperAdminPermissions,
            Roles.TenantOwner => TenantOwnerPermissions,
            Roles.TenantAdmin => TenantAdminPermissions,
            Roles.TenantManager => TenantManagerPermissions,
            Roles.TenantUser => TenantUserPermissions,
            Roles.ReadOnly => ReadOnlyPermissions,
            Roles.Occupant => OccupantPermissions,
            Roles.OccupantAdmin => OccupantAdminPermissions,
            Roles.OccupantOwner => OccupantOwnerPermissions,
            _ => Array.Empty<string>()
        };
    }
}
