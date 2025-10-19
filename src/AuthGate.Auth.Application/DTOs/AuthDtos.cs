
namespace AuthGate.Auth.Application.DTOs;

public record ResetPasswordRequest(string Token, string NewPassword);
public record MfaEnableRequest(string Issuer = "AuthGate"); // "TOTP" | "SMS"
public record MfaVerifyRequest(string Code, string? DeviceId = null);

public record UserDto(Guid Id, string Email, string FullName, bool MfaEnabled, bool IsLocked, bool IsDeleted);
public record TokensDto(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);
public record LoginResponse(UserDto User, TokensDto Tokens, string Status = "Success");

public record DeviceSessionDto(Guid Id, string IpAddress, string UserAgent, DateTime CreatedAtUtc, DateTime ExpiresAtUtc, bool Active);

