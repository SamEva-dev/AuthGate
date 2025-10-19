using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Application.Interfaces;
using AuthGate.Auth.Domain.Entities;
using MediatR;

namespace AuthGate.Auth.Application.Features.Password;

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;

    public ForgotPasswordHandler(IUnitOfWork uow, IEmailService email, IAuditService audit)
    { 
        _uow = uow; 
        _email = email; 
        _audit = audit; 
    }

    public async Task Handle(ForgotPasswordCommand req, CancellationToken cancellationToken)
    {
        var user = await _uow.Auth.GetByEmailAsync(req.Email);
        if (user is null) return; // on ne révèle pas

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("=", "").Replace("+", "").Replace("/", "");
        var entity = new PasswordResetToken
        {
            UserId = user.Id,
            Token = token,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(2),
            IpAddress = req.GetIp(),
            UserAgent = req.GetUserAgent()
        };

        await _uow.Auth.AddPasswordResetTokenAsync(entity);

        await _email.SendPasswordResetAsync(user.Email, token);

        await _audit.LogAsync("PasswordForgot", "Reset token generated", user.Id.ToString(), user.Email, req.GetIp(), req.GetUserAgent());
    }
}