namespace AuthGate.Auth.Domain.Enums;

/// <summary>
/// Represents the status of a user invitation
/// </summary>
public enum InvitationStatus
{
    /// <summary>
    /// Invitation is pending acceptance
    /// </summary>
    Pending,

    /// <summary>
    /// Invitation has been accepted by the user
    /// </summary>
    Accepted,

    /// <summary>
    /// Invitation has expired (past ExpiresAt date)
    /// </summary>
    Expired,

    /// <summary>
    /// Invitation was cancelled by the inviter
    /// </summary>
    Cancelled
}
