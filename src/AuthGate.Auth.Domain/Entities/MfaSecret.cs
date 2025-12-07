using AuthGate.Auth.Domain.Common;

namespace AuthGate.Auth.Domain.Entities;

/// <summary>
/// Represents the MFA (TOTP) secret for a user with recovery codes support
/// </summary>
public class MfaSecret : BaseEntity
{
    /// <summary>
    /// Gets or sets the ID of the user this MFA secret belongs to
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the encrypted secret key used for TOTP generation
    /// </summary>
    public required string EncryptedSecret { get; set; }

    /// <summary>
    /// Gets or sets whether this MFA secret has been verified by the user
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Gets or sets the date and time when MFA was verified (UTC)
    /// </summary>
    public DateTime? VerifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets whether MFA is currently enabled for this user
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the date and time when MFA was enabled (UTC)
    /// </summary>
    public DateTime? EnabledAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the date and time when MFA was last used (UTC)
    /// </summary>
    public DateTime? LastUsedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the recovery codes (comma-separated encrypted)
    /// </summary>
    public string? RecoveryCodes { get; set; }

    /// <summary>
    /// Gets or sets the number of recovery codes remaining
    /// </summary>
    public int RecoveryCodesRemaining { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the user this MFA secret belongs to
    /// </summary>
    public virtual User User { get; set; } = null!;

    // Business logic methods

    public void Enable()
    {
        IsEnabled = true;
        IsVerified = true;
        EnabledAtUtc = DateTime.UtcNow;
        VerifiedAtUtc ??= DateTime.UtcNow;
    }

    public void Disable()
    {
        IsEnabled = false;
        EnabledAtUtc = null;
    }

    public void UpdateLastUsed()
    {
        LastUsedAtUtc = DateTime.UtcNow;
    }

    public List<string> GetRecoveryCodesList()
    {
        if (string.IsNullOrEmpty(RecoveryCodes))
            return new List<string>();

        return RecoveryCodes.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    public void SetRecoveryCodesList(List<string> codes)
    {
        RecoveryCodes = string.Join(',', codes);
        RecoveryCodesRemaining = codes.Count;
    }

    public bool UseRecoveryCode(string code)
    {
        var codes = GetRecoveryCodesList();
        if (!codes.Contains(code, StringComparer.OrdinalIgnoreCase))
            return false;

        codes.Remove(code);
        SetRecoveryCodesList(codes);
        return true;
    }
}
