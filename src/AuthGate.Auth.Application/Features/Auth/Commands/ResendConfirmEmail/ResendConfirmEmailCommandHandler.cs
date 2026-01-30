using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Enums;
using AuthGate.Auth.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AuthGate.Auth.Application.Features.Auth.Commands.ResendConfirmEmail;

public class ResendConfirmEmailCommandHandler : IRequestHandler<ResendConfirmEmailCommand, Result<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ResendConfirmEmailCommandHandler(
        UserManager<User> userManager,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(ResendConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var email = (request.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<bool>("Email is required.");

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return Result.Success(true);
        }

        if (user.EmailConfirmed)
        {
            return Result.Success(true);
        }

        var outboxMessage = OutboxMessage.Create(
            OutboxMessageType.SendConfirmEmail,
            "{}",
            user.Id,
            Guid.NewGuid().ToString("N"));

        await _outboxRepository.AddAsync(outboxMessage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
