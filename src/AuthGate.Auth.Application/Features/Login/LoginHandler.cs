
using AuthGate.Auth.Application.DTOs;
using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Application.Interfaces;
using AuthGate.Auth.Domain.Entities;
using Microsoft.Extensions.Logging;
using Serilog;
using MediatR;

namespace AuthGate.Auth.Application.Features.Login;

public class LoginHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;
    private readonly ILogger<LoginHandler> _logger;
    private readonly IAuditService _audit;

    public LoginHandler(IUnitOfWork uow, IPasswordHasher hasher, IJwtService jwt, IAuditService audit, ILogger<LoginHandler> logger)
    {
        _uow = uow;
        _hasher = hasher;
        _jwt = jwt;
        _logger = logger;
        _audit = audit;
    }

    public async Task<LoginResponse> Handle(LoginCommand req, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔐 Login attempt for {Email} from {Ip}", req.Email, req.GetIp());

        try
        {
            var user = await _uow.Auth.GetByEmailAsync(req.Email);
            if (user == null || user.IsDeleted)
            {
                _logger.LogWarning("Login failed: user not found {Email}", req.Email);
                await _audit.LogAsync("LoginFailed", $"Login failed: user not found", null, req.Email, req.GetIp(), req.GetUserAgent());
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            // Lockout check
            if (user.IsLocked && user.LockoutEndUtc > DateTime.UtcNow)
                throw new InvalidOperationException("Account locked.");

            var success = _hasher.Verify(req.Password, user.PasswordHash);
            _logger.LogInformation("Password check for {Email} => {Success}", req.Email, success);


            user.LoginAttempts.Add(new UserLoginAttempt
            {
                UserId = user.Id,
                Email = user.Email,
                Success = success,
                IpAddress = req.GetIp()
            });

            if (!success)
            {
                _logger.LogWarning("❌ Login failed: invalid password for {Email}", req.Email);
                await _audit.LogAsync("LoginFailed", $"Invalid password", user.Id.ToString(), req.Email, req.GetIp(), req.GetUserAgent());
                var fails = user.LoginAttempts
                .Where(a => !a.Success && a.AttemptedAtUtc > DateTime.UtcNow.AddMinutes(-15))
                .Count();

                if (fails >= 5)
                {
                    user.IsLocked = true;
                    user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(30);
                }

                await _uow.SaveChangesAsync();
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            if (user.MfaEnabled)
            {
                _logger.LogInformation("🔐 MFA required for {Email}", user.Email);
                await _audit.LogAsync("MfaRequired", "MFA step required", user.Id.ToString(), user.Email);
                return new LoginResponse(
                    new UserDto(user.Id, user.Email, user.FullName, user.MfaEnabled, user.IsLocked, user.IsDeleted),
                    null,
                    "MfaRequired"
                );
            }

            // Reset lockout
            user.IsLocked = false;
            user.LockoutEndUtc = null;

            var (access, refresh, expires) = _jwt.GenerateTokens(user);

            var device = new DeviceSession
            {
                UserId = user.Id,
                RefreshToken = refresh,
                UserAgent = req.GetUserAgent(),
                IpAddress = req.GetIp(),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
            };

            //await _uow.Auth.AddDeviceSessionAsync(device);

            _logger.LogInformation("✅ Login success for {Email}. Access expires {Expires}", req.Email, expires);
           // await _audit.LogAsync("LoginSuccess", "User logged in successfully", user.Id.ToString(), user.Email, req.GetIp(), req.GetUserAgent());


            // ✅ Une seule transaction pour tout
           //await _uow.SaveChangesAsync();

            return new LoginResponse(
                new UserDto(user.Id, user.Email, user.FullName, user.MfaEnabled, user.IsLocked, user.IsDeleted),
                new TokensDto(access, refresh, expires)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [LoginHandler] Error processing login for {Email}", req.Email);
            throw;
        }
        finally
        {
            _logger.LogInformation("🏁 [LoginHandler] End for {Email}", req.Email);
        }
    }
}