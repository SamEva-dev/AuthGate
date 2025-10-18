
namespace AuthGate.Auth.Application.DTOs;

public record LoginRequest(string Email, string Password, string? Code = null);
public record RegisterRequest(string FullName, string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
public record MfaEnableRequest(string Type); // "TOTP" | "SMS"
public record MfaVerifyRequest(string Code, string? DeviceId = null);

public record UserDto(Guid Id, string Email, string FullName, bool MfaEnabled);
public record TokensDto(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);
public record LoginResponse(UserDto User, TokensDto Tokens);