using AuthGate.Auth.Application.DTOs;
using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Application.Interfaces;
using MediatR;

namespace AuthGate.Auth.Application.Features.Password;

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditService _audit;

    public ResetPasswordHandler(IUnitOfWork uow, IPasswordHasher hasher, IAuditService audit)
    { 
        _uow = uow; 
        _hasher = hasher; 
        _audit = audit; 
    }

    public async Task Handle(ResetPasswordCommand req, CancellationToken cancellationToken)
    {
        var token = await _uow.Auth.GetValidResetTokenAsync(req.Token)
                    ?? throw new UnauthorizedAccessException("Invalid or expired token");

        var user = await _uow.Auth.GetByIdAsync(token.UserId)
                    ?? throw new UnauthorizedAccessException("User not found");

        user.PasswordHash = _hasher.Hash(req.NewPassword);
        await _uow.Auth.MarkResetTokenUsedAsync(token);
        await _uow.SaveChangesAsync();

        await _audit.LogAsync("PasswordReset", "Password changed via reset token", user.Id.ToString(), user.Email, req.GetIp(), req.GetUserAgent());
    }
}