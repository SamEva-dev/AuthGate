namespace AuthGate.Auth.Domain.Constants;

/// <summary>
/// Defines the application permission codes
/// </summary>
public static class Permissions
{
    // ========================================
    // TENANT MANAGEMENT
    // ========================================
    
    /// <summary>
    /// Can view tenant settings and configuration
    /// </summary>
    public const string TenantSettingsRead = "tenant.settings.read";

    /// <summary>
    /// Can modify tenant settings and configuration
    /// </summary>
    public const string TenantSettingsWrite = "tenant.settings.write";

    /// <summary>
    /// Can delete tenant (dangerous operation)
    /// </summary>
    public const string TenantDelete = "tenant.delete";

    // ========================================
    // BILLING & SUBSCRIPTION
    // ========================================
    
    /// <summary>
    /// Can view billing information and invoices
    /// </summary>
    public const string BillingRead = "billing.read";

    /// <summary>
    /// Can manage subscriptions and payment methods
    /// </summary>
    public const string BillingWrite = "billing.write";

    // ========================================
    // USER MANAGEMENT
    // ========================================
    
    /// <summary>
    /// Can view users in the tenant
    /// </summary>
    public const string UsersRead = "users.read";

    /// <summary>
    /// Can create and update users
    /// </summary>
    public const string UsersWrite = "users.write";

    /// <summary>
    /// Can delete users
    /// </summary>
    public const string UsersDelete = "users.delete";

    /// <summary>
    /// Can invite new users to the tenant
    /// </summary>
    public const string UsersInvite = "users.invite";

    // ========================================
    // ROLE MANAGEMENT
    // ========================================
    
    /// <summary>
    /// Can view roles and their permissions
    /// </summary>
    public const string RolesRead = "roles.read";

    /// <summary>
    /// Can assign roles to users
    /// </summary>
    public const string RolesAssign = "roles.assign";

    /// <summary>
    /// Can create and modify custom roles
    /// </summary>
    public const string RolesWrite = "roles.write";

    // ========================================
    // PROPERTIES MANAGEMENT
    // ========================================
    
    /// <summary>
    /// Can view properties
    /// </summary>
    public const string PropertiesRead = "properties.read";

    /// <summary>
    /// Can create and update properties
    /// </summary>
    public const string PropertiesWrite = "properties.write";

    /// <summary>
    /// Can delete properties
    /// </summary>
    public const string PropertiesDelete = "properties.delete";

    // ========================================
    // TENANTS (Locataires) MANAGEMENT
    // ========================================
    
    /// <summary>
    /// Can view tenants (locataires)
    /// </summary>
    public const string TenantsRead = "tenants.read";

    /// <summary>
    /// Can create and update tenants
    /// </summary>
    public const string TenantsWrite = "tenants.write";

    /// <summary>
    /// Can delete tenants
    /// </summary>
    public const string TenantsDelete = "tenants.delete";

    // ========================================
    // CONTRACTS MANAGEMENT
    // ========================================
    
    /// <summary>
    /// Can view contracts
    /// </summary>
    public const string ContractsRead = "contracts.read";

    /// <summary>
    /// Can create and update contracts
    /// </summary>
    public const string ContractsWrite = "contracts.write";

    /// <summary>
    /// Can terminate contracts
    /// </summary>
    public const string ContractsTerminate = "contracts.terminate";

    /// <summary>
    /// Can delete contracts
    /// </summary>
    public const string ContractsDelete = "contracts.delete";

    // ========================================
    // DOCUMENTS MANAGEMENT
    // ========================================
    
    /// <summary>
    /// Can view documents
    /// </summary>
    public const string DocumentsRead = "documents.read";

    public const string DocumentsWrite = "documents.write";

    /// <summary>
    /// Can upload new documents
    /// </summary>
    public const string DocumentsUpload = "documents.upload";

    /// <summary>
    /// Can generate documents from templates
    /// </summary>
    public const string DocumentsGenerate = "documents.generate";

    /// <summary>
    /// Can delete documents
    /// </summary>
    public const string DocumentsDelete = "documents.delete";

    // ========================================
    // ROOMS MANAGEMENT (LocaGuest)
    // ========================================

    public const string RoomsRead = "rooms.read";

    public const string RoomsWrite = "rooms.write";

    // ========================================
    // PAYMENTS MANAGEMENT (LocaGuest)
    // ========================================

    public const string PaymentsRead = "payments.read";

    public const string PaymentsWrite = "payments.write";

    // ========================================
    // DEPOSITS MANAGEMENT (LocaGuest)
    // ========================================

    public const string DepositsRead = "deposits.read";

    public const string DepositsWrite = "deposits.write";

    // ========================================
    // TEAM MANAGEMENT (LocaGuest)
    // ========================================

    public const string TeamRead = "team.read";

    public const string TeamManage = "team.manage";

    // ========================================
    // ANALYTICS & REPORTING
    // ========================================
    
    /// <summary>
    /// Can view analytics and reports
    /// </summary>
    public const string AnalyticsRead = "analytics.read";

    /// <summary>
    /// Can export data and reports
    /// </summary>
    public const string AnalyticsExport = "analytics.export";

    // ========================================
    // LOGS & AUDIT
    // ========================================
    
    /// <summary>
    /// Can view audit logs
    /// </summary>
    public const string AuditLogsRead = "audit.logs.read";

    /// <summary>
    /// Can view system logs (SuperAdmin only)
    /// </summary>
    public const string SystemLogsRead = "system.logs.read";

    // ========================================
    // ALL PERMISSIONS
    // ========================================

    /// <summary>
    /// All available permissions in the system
    /// </summary>
    public static readonly string[] All = 
    {
        // Tenant Management
        TenantSettingsRead,
        TenantSettingsWrite,
        TenantDelete,

        // Billing
        BillingRead,
        BillingWrite,

        // User Management
        UsersRead,
        UsersWrite,
        UsersDelete,
        UsersInvite,

        // Role Management
        RolesRead,
        RolesAssign,
        RolesWrite,

        // Properties
        PropertiesRead,
        PropertiesWrite,
        PropertiesDelete,

        // Tenants (Locataires)
        TenantsRead,
        TenantsWrite,
        TenantsDelete,

        // Contracts
        ContractsRead,
        ContractsWrite,
        ContractsTerminate,
        ContractsDelete,

        // Documents
        DocumentsRead,
        DocumentsWrite,
        DocumentsUpload,
        DocumentsGenerate,
        DocumentsDelete,

        // Rooms
        RoomsRead,
        RoomsWrite,

        // Payments
        PaymentsRead,
        PaymentsWrite,

        // Deposits
        DepositsRead,
        DepositsWrite,

        // Team
        TeamRead,
        TeamManage,

        // Analytics
        AnalyticsRead,
        AnalyticsExport,

        // Audit & Logs
        AuditLogsRead,
        SystemLogsRead
    };
}
