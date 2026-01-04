namespace AuthGate.Auth.Application.DTOs.Auth;

public sealed class PreLoginRequestDto
{
    public string Email { get; set; } = string.Empty;
}

public sealed class PreLoginResponseDto
{
    public string NextStep { get; set; } = string.Empty;
    public string? Error { get; set; }
}
