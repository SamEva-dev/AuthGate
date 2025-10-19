using AuthGate.Auth.Application.DTOs;
using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Application.Interfaces;
using AuthGate.Auth.Domain.Entities;
using Microsoft.Extensions.Logging;
using MediatR;

namespace AuthGate.Auth.Application.Features.Register;

public class RegisterHandler : IRequestHandler<RegisterCommand, LoginResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;
    private readonly IEmailService _email;
    private readonly ILogger<RegisterHandler> _logger;
    private readonly IAuditService _audit;

    public RegisterHandler(IUnitOfWork uow, IPasswordHasher hasher, IJwtService jwt, IEmailService email, IAuditService audit, ILogger<RegisterHandler> logger)
    {
        _uow = uow;
        _hasher = hasher;
        _jwt = jwt;
        _email = email;
        _logger = logger;
        _audit = audit;
    }

    public async Task<LoginResponse> Handle(RegisterCommand req, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📩 Register attempt for {Email} from {Ip}", req.Email, req.GetIp());

        try
        {


            var existing = await _uow.Auth.GetByEmailAsync(req.Email);
            if (existing != null)
            {
                _logger.LogWarning("❌ Registration failed: email {Email} already exists", req.Email);
                await _audit.LogAsync("❌ Registration failed: email {Email} already exists", req.Email);

                throw new InvalidOperationException("Email already exists.");
            }

            var user = new User
            {
                Email = req.Email,
                FullName = req.FullName,
                PasswordHash = _hasher.Hash(req.Password),
                IsEmailValidated = false
            };

            await _uow.Auth.AddUserAsync(user);

            var (access, refresh, expires) = _jwt.GenerateTokens(user);

            var device = new DeviceSession
            {
                UserId = user.Id,
                RefreshToken = refresh,
                UserAgent = req.GetUserAgent(),
                IpAddress = req.GetIp(),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
            };

            await _uow.Auth.AddDeviceSessionAsync(device);

            // ✅ Une seule transaction
            await _uow.SaveChangesAsync();

            _logger.LogInformation("✅ User {Email} registered successfully at {Time}", req.Email, DateTime.UtcNow);


            await _email.SendValidationEmailAsync(user.Email, Guid.NewGuid().ToString("N"));

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