namespace AuthGate.Auth.Application.DTOs.Auth;

public sealed class CreateManagerInvitationRequestDto
{
    public string Email { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public List<Guid> RoleIds { get; set; } = new();
    public int ExpiresInHours { get; set; } = 48;
    public bool SendEmail { get; set; } = true;
}

public sealed class CreateManagerInvitationResponseDto
{
    public Guid InvitationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public List<Guid> RoleIds { get; set; } = new();
    public string Type { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}

public sealed class ManagerInvitationListItemDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public List<Guid> RoleIds { get; set; } = new();
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ValidateManagerInvitationResponseDto
{
    public string Email { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public List<Guid> RoleIds { get; set; } = new();
    public DateTime ExpiresAtUtc { get; set; }
    public string Type { get; set; } = string.Empty;
}

public sealed class AcceptManagerInvitationRequestDto
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class RevokeManagerInvitationResponseDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
}
