namespace AuthGate.Auth.Domain.Constants;

/// <summary>
/// Defines the application role names
/// </summary>
public static class Roles
{
    /// <summary>
    /// SuperAdmin - Rôle technique réservé à l'équipe plateforme
    /// Accès global à tous les tenants, logs, administration interne
    /// </summary>
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>
    /// TenantOwner - Propriétaire du compte/organisation
    /// Celui qui paye, gère la facturation et l'organisation
    /// Accès complet à son tenant
    /// </summary>
    public const string TenantOwner = "TenantOwner";

    /// <summary>
    /// TenantAdmin - Administrateur désigné par l'Owner
    /// Peut gérer les propriétés, locataires, contrats et utilisateurs
    /// Pas d'accès à la facturation
    /// </summary>
    public const string TenantAdmin = "TenantAdmin";

    /// <summary>
    /// TenantManager - Gestionnaire immobilier
    /// Accès opérationnel avancé mais sans droits d'administration
    /// </summary>
    public const string TenantManager = "TenantManager";

    /// <summary>
    /// TenantUser - Utilisateur standard du tenant
    /// Assistant, agent, consultant avec accès limité
    /// </summary>
    public const string TenantUser = "TenantUser";

    /// <summary>
    /// ReadOnly - Accès lecture seule
    /// Comptable externe, auditeur, etc.
    /// </summary>
    public const string ReadOnly = "ReadOnly";

    /// <summary>
    /// All role names
    /// </summary>
    public static readonly string[] All = 
    {
        SuperAdmin,
        TenantOwner,
        TenantAdmin,
        TenantManager,
        TenantUser,
        ReadOnly
    };

    /// <summary>
    /// System roles that cannot be deleted
    /// </summary>
    public static readonly string[] SystemRoles = 
    {
        SuperAdmin
    };

    /// <summary>
    /// Administrative roles with elevated permissions
    /// </summary>
    public static readonly string[] AdminRoles = 
    {
        SuperAdmin,
        TenantOwner,
        TenantAdmin
    };

    /// <summary>
    /// Operational roles (can manage business entities)
    /// </summary>
    public static readonly string[] OperationalRoles = 
    {
        SuperAdmin,
        TenantOwner,
        TenantAdmin,
        TenantManager
    };
}
