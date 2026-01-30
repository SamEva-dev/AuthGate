using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Enums;
using AuthGate.Auth.Domain.Repositories;
using AuthGate.Auth.Application.Features.Auth.Commands.RegisterWithTenant;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Auth.Commands.ValidateEmail;

public sealed class ValidateEmailCommandHandler : IRequestHandler<ValidateEmailCommand, Result<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ValidateEmailCommandHandler> _logger;

    public ValidateEmailCommandHandler(
        UserManager<User> userManager,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork,
        ILogger<ValidateEmailCommandHandler> logger)
    {
        _userManager = userManager;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ValidateEmailCommand request, CancellationToken cancellationToken)
    {
        var email = (request.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<bool>("Email is required");

        var token = request.Token ?? string.Empty;
        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure<bool>("Token is required");

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return Result.Failure<bool>("Invalid token");

        if (user.EmailConfirmed)
            return Result.Success(true);

        var res = await _userManager.ConfirmEmailAsync(user, token);
        if (!res.Succeeded)
        {
            var err = string.Join(", ", res.Errors.Select(e => e.Description));
            _logger.LogWarning("Email confirmation failed for {Email}: {Errors}", email, err);
            return Result.Failure<bool>("Invalid token");
        }

        if (user.OrganizationId == null || user.OrganizationId == Guid.Empty)
        {
            if (string.IsNullOrWhiteSpace(user.PendingOrganizationName))
            {
                _logger.LogWarning("Email confirmed for {Email} but missing PendingOrganizationName for user {UserId}", email, user.Id);
                return Result.Failure<bool>("Email confirmé, mais les informations de création d'organisation sont manquantes. Veuillez recommencer l'inscription.");
            }

            user.Status = UserStatus.PendingProvisioning;
            user.IsActive = false;

            var payload = new ProvisionOrganizationPayload
            {
                UserId = user.Id,
                Email = user.Email ?? email,
                OrganizationName = user.PendingOrganizationName,
                Phone = user.PendingOrganizationPhone,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty
            };

            var outbox = OutboxMessage.Create(
                OutboxMessageType.ProvisionOrganization,
                System.Text.Json.JsonSerializer.Serialize(payload),
                user.Id,
                Guid.NewGuid().ToString("N"));

            await _outboxRepository.AddAsync(outbox, cancellationToken);

            var updateRes = await _userManager.UpdateAsync(user);
            if (!updateRes.Succeeded)
            {
                var errors = string.Join(", ", updateRes.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to update user after email confirmation for {Email}: {Errors}", email, errors);
                return Result.Failure<bool>("Email confirmed but provisioning could not start. Please retry.");
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Email confirmed for {Email}", email);
        return Result.Success(true);
    }
}
