using AuthGate.Auth.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace AuthGate.Auth.Configuration;

/// <summary>
/// Defines authorization policies based on permissions
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Policy names
    /// </summary>
    public static class PolicyNames
    {
        // Tenant Management Policies
        public const string ManageTenantSettings = "ManageTenantSettings";
        public const string ViewTenantSettings = "ViewTenantSettings";
        public const string DeleteTenant = "DeleteTenant";

        // Billing Policies
        public const string ManageBilling = "ManageBilling";
        public const string ViewBilling = "ViewBilling";

        // User Management Policies
        public const string ManageUsers = "ManageUsers";
        public const string ViewUsers = "ViewUsers";
        public const string InviteUsers = "InviteUsers";

        // Role Management Policies
        public const string ManageRoles = "ManageRoles";
        public const string AssignRoles = "AssignRoles";
        public const string ViewRoles = "ViewRoles";

        // Properties Policies
        public const string ManageProperties = "ManageProperties";
        public const string ViewProperties = "ViewProperties";

        // Tenants (Locataires) Policies
        public const string ManageTenants = "ManageTenants";
        public const string ViewTenants = "ViewTenants";

        // Contracts Policies
        public const string ManageContracts = "ManageContracts";
        public const string ViewContracts = "ViewContracts";
        public const string TerminateContracts = "TerminateContracts";

        // Documents Policies
        public const string ManageDocuments = "ManageDocuments";
        public const string ViewDocuments = "ViewDocuments";
        public const string GenerateDocuments = "GenerateDocuments";

        // Analytics Policies
        public const string ViewAnalytics = "ViewAnalytics";
        public const string ExportAnalytics = "ExportAnalytics";

        // Audit Policies
        public const string ViewAuditLogs = "ViewAuditLogs";
        public const string ViewSystemLogs = "ViewSystemLogs";

        // Role-based Policies
        public const string IsSuperAdmin = "IsSuperAdmin";
        public const string IsTenantOwner = "IsTenantOwner";
        public const string IsTenantAdmin = "IsTenantAdmin";
        public const string IsAdminOrOwner = "IsAdminOrOwner";
    }

    /// <summary>
    /// Configures authorization policies
    /// </summary>
    public static void AddAuthorizationPolicies(this AuthorizationOptions options)
    {
        // ========================================
        // PERMISSION-BASED POLICIES
        // ========================================

        // Tenant Management
        options.AddPolicy(PolicyNames.ManageTenantSettings, policy =>
            policy.RequireClaim("permission", Permissions.TenantSettingsWrite));
        
        options.AddPolicy(PolicyNames.ViewTenantSettings, policy =>
            policy.RequireClaim("permission", Permissions.TenantSettingsRead));
        
        options.AddPolicy(PolicyNames.DeleteTenant, policy =>
            policy.RequireClaim("permission", Permissions.TenantDelete));

        // Billing
        options.AddPolicy(PolicyNames.ManageBilling, policy =>
            policy.RequireClaim("permission", Permissions.BillingWrite));
        
        options.AddPolicy(PolicyNames.ViewBilling, policy =>
            policy.RequireClaim("permission", Permissions.BillingRead));

        // User Management
        options.AddPolicy(PolicyNames.ManageUsers, policy =>
            policy.RequireClaim("permission", Permissions.UsersWrite));
        
        options.AddPolicy(PolicyNames.ViewUsers, policy =>
            policy.RequireClaim("permission", Permissions.UsersRead));
        
        options.AddPolicy(PolicyNames.InviteUsers, policy =>
            policy.RequireClaim("permission", Permissions.UsersInvite));

        // Role Management
        options.AddPolicy(PolicyNames.ManageRoles, policy =>
            policy.RequireClaim("permission", Permissions.RolesWrite));
        
        options.AddPolicy(PolicyNames.AssignRoles, policy =>
            policy.RequireClaim("permission", Permissions.RolesAssign));
        
        options.AddPolicy(PolicyNames.ViewRoles, policy =>
            policy.RequireClaim("permission", Permissions.RolesRead));

        // Properties
        options.AddPolicy(PolicyNames.ManageProperties, policy =>
            policy.RequireClaim("permission", Permissions.PropertiesWrite));
        
        options.AddPolicy(PolicyNames.ViewProperties, policy =>
            policy.RequireClaim("permission", Permissions.PropertiesRead));

        // Tenants (Locataires)
        options.AddPolicy(PolicyNames.ManageTenants, policy =>
            policy.RequireClaim("permission", Permissions.TenantsWrite));
        
        options.AddPolicy(PolicyNames.ViewTenants, policy =>
            policy.RequireClaim("permission", Permissions.TenantsRead));

        // Contracts
        options.AddPolicy(PolicyNames.ManageContracts, policy =>
            policy.RequireClaim("permission", Permissions.ContractsWrite));
        
        options.AddPolicy(PolicyNames.ViewContracts, policy =>
            policy.RequireClaim("permission", Permissions.ContractsRead));
        
        options.AddPolicy(PolicyNames.TerminateContracts, policy =>
            policy.RequireClaim("permission", Permissions.ContractsTerminate));

        // Documents
        options.AddPolicy(PolicyNames.ManageDocuments, policy =>
            policy.RequireClaim("permission", Permissions.DocumentsUpload));
        
        options.AddPolicy(PolicyNames.ViewDocuments, policy =>
            policy.RequireClaim("permission", Permissions.DocumentsRead));
        
        options.AddPolicy(PolicyNames.GenerateDocuments, policy =>
            policy.RequireClaim("permission", Permissions.DocumentsGenerate));

        // Analytics
        options.AddPolicy(PolicyNames.ViewAnalytics, policy =>
            policy.RequireClaim("permission", Permissions.AnalyticsRead));
        
        options.AddPolicy(PolicyNames.ExportAnalytics, policy =>
            policy.RequireClaim("permission", Permissions.AnalyticsExport));

        // Audit
        options.AddPolicy(PolicyNames.ViewAuditLogs, policy =>
            policy.RequireClaim("permission", Permissions.AuditLogsRead));
        
        options.AddPolicy(PolicyNames.ViewSystemLogs, policy =>
            policy.RequireClaim("permission", Permissions.SystemLogsRead));

        // ========================================
        // ROLE-BASED POLICIES
        // ========================================

        options.AddPolicy(PolicyNames.IsSuperAdmin, policy =>
            policy.RequireRole(Roles.SuperAdmin));

        options.AddPolicy(PolicyNames.IsTenantOwner, policy =>
            policy.RequireRole(Roles.TenantOwner));

        options.AddPolicy(PolicyNames.IsTenantAdmin, policy =>
            policy.RequireRole(Roles.TenantAdmin));

        options.AddPolicy(PolicyNames.IsAdminOrOwner, policy =>
            policy.RequireRole(Roles.SuperAdmin, Roles.TenantOwner, Roles.TenantAdmin));
    }
}
