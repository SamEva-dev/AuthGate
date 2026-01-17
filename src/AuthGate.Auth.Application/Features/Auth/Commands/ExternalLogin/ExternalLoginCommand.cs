using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.DTOs.Auth;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.ExternalLogin;

/// <summary>
/// Command to authenticate a user via external OAuth provider token
/// </summary>
public class ExternalLoginCommand : IRequest<Result<ExternalLoginResponseDto>>
{
    /// <summary>
    /// OAuth provider name (google, facebook)
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// ID token (Google) or access token (Facebook) from the OAuth provider
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// Email extracted from the token (for validation)
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// First name from the provider
    /// </summary>
    public string? FirstName { get; set; }
    
    /// <summary>
    /// Last name from the provider
    /// </summary>
    public string? LastName { get; set; }
    
    /// <summary>
    /// Provider's unique user ID
    /// </summary>
    public string? ProviderId { get; set; }
    
    /// <summary>
    /// User's profile picture URL
    /// </summary>
    public string? PictureUrl { get; set; }
}
