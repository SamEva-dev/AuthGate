namespace AuthGate.Auth.Application.DTOs.Auth;

/// <summary>
/// DTO for MFA verification request
/// </summary>
public record VerifyMfaDto
{
    /// <summary>
    /// Temporary MFA token received during login
    /// </summary>
    public required string MfaToken { get; init; }

    /// <summary>
    /// 6-digit TOTP code from authenticator app
    /// </summary>
    public required string Code { get; init; }
}
