namespace AuthGate.Auth.Application.DTOs.Auth;

/// <summary>
/// Request to initiate external OAuth login
/// </summary>
public class ExternalLoginRequestDto
{
    /// <summary>
    /// OAuth provider (google, facebook)
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Frontend URL to redirect after successful login
    /// </summary>
    public string? ReturnUrl { get; set; }
}

/// <summary>
/// Request containing the OAuth token from the provider
/// </summary>
public class ExternalTokenLoginDto
{
    /// <summary>
    /// OAuth provider (google, facebook)
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// ID token or access token from the OAuth provider
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional: Access token for Facebook (which uses access tokens, not ID tokens)
    /// </summary>
    public string? AccessToken { get; set; }
}

/// <summary>
/// Response after successful external login
/// </summary>
public class ExternalLoginResponseDto
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public ExternalUserInfoDto? User { get; set; }
    public string? Error { get; set; }
    
    /// <summary>
    /// True if this is a new user registration
    /// </summary>
    public bool IsNewUser { get; set; }
    
    /// <summary>
    /// True if user needs to complete registration (select organization, etc.)
    /// </summary>
    public bool RequiresRegistration { get; set; }
}

/// <summary>
/// User info extracted from external provider
/// </summary>
public class ExternalUserInfoDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PictureUrl { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Available OAuth providers response
/// </summary>
public class OAuthProvidersResponseDto
{
    public List<OAuthProviderInfo> Providers { get; set; } = new();
}

public class OAuthProviderInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string? IconUrl { get; set; }
}

/// <summary>
/// OAuth configuration for frontend
/// </summary>
public class OAuthConfigDto
{
    public string? ClientId { get; set; }
    public string? AppId { get; set; }
    public string Scope { get; set; } = string.Empty;
}

/// <summary>
/// Internal model for external user info from OAuth provider
/// </summary>
public class ExternalUserInfo
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string? PictureUrl { get; set; }
}
