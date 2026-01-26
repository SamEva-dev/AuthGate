namespace AuthGate.Auth.Application.DTOs.Manager;

public sealed class ManagerBootstrapDto
{
    public BootstrapOrganizationDto Organization { get; set; } = new();
    public BootstrapSecuritySettingsDto SecuritySettings { get; set; } = new();
    public int PermissionsCatalogVersion { get; set; }
    public List<BootstrapPermissionDto> PermissionsCatalog { get; set; } = new();
    public List<string> MyEffectivePermissions { get; set; } = new();
    public List<string> MyRoles { get; set; } = new();
    public BootstrapKpisDto Kpis { get; set; } = new();
}

public sealed class BootstrapOrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class BootstrapSecuritySettingsDto
{
    public bool MfaRequiredForAdmins { get; set; }
    public bool MfaRequiredForAll { get; set; }
    public int InvitationExpiresHours { get; set; }
    public List<string> AllowedEmailDomains { get; set; } = new();
}

public sealed class BootstrapPermissionDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
}

public sealed class BootstrapKpisDto
{
    public int UsersTotal { get; set; }
    public int UsersActive { get; set; }
    public int InvitationsPending { get; set; }
    public int AdminsCount { get; set; }
}
