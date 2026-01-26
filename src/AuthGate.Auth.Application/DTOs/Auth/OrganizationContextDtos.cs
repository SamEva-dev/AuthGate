namespace AuthGate.Auth.Application.DTOs.Auth;

public sealed class UserOrganizationDto
{
    public Guid OrganizationId { get; set; }
    public string? Name { get; set; }
    public string? Role { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class SwitchOrganizationRequestDto
{
    public Guid OrganizationId { get; set; }
}

public sealed class SwitchOrganizationResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public Guid? OrganizationId { get; set; }
}
