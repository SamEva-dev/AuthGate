
using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Application.Interfaces;

public interface IJwtService
{
    (string accessToken, string refreshToken, DateTime expiresAtUtc) GenerateTokens(User user);
}