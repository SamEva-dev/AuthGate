namespace AuthGate.Auth.Application.Common.Interfaces;

/// <summary>
/// Service interface for accessing HTTP context information
/// </summary>
public interface IHttpContextAccessor
{
    /// <summary>
    /// Gets the IP address of the current request
    /// </summary>
    string? GetIpAddress();

    /// <summary>
    /// Gets the user agent of the current request
    /// </summary>
    string? GetUserAgent();

    /// <summary>
    /// Gets the current user ID from the claims
    /// </summary>
    Guid? GetCurrentUserId();
}
