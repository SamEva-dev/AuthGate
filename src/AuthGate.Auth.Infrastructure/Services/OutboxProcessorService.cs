using System.Text.Json;
using AuthGate.Auth.Application.Common.Clients;
using AuthGate.Auth.Application.Common.Clients.Models;
using AuthGate.Auth.Application.Features.Auth.Commands.RegisterWithTenant;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Enums;
using AuthGate.Auth.Domain.Repositories;
using AuthGate.Auth.Infrastructure.Options;
using AuthGate.Auth.Infrastructure.Persistence;
using LocaGuest.Emailing.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthGate.Auth.Infrastructure.Services;

/// <summary>
/// Background service that processes outbox messages for reliable async operations.
/// Implements the Outbox Pattern with exponential backoff retry.
/// </summary>
public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly OutboxProcessorOptions _options;

    public OutboxProcessorService(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorService> logger,
        IOptions<OutboxProcessorOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessorService starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OutboxProcessorService main loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("OutboxProcessorService stopping...");
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var messages = await outboxRepository.GetPendingMessagesAsync(_options.BatchSize, cancellationToken);

        if (messages.Count == 0)
            return;

        _logger.LogDebug("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                await ProcessMessageAsync(scope.ServiceProvider, message, cancellationToken);
                message.MarkAsProcessed();
                _logger.LogInformation(
                    "Outbox message {MessageId} processed successfully. Type: {Type}",
                    message.Id, message.Type);
            }
            catch (Exception ex)
            {
                message.RecordFailure(ex.Message);
                _logger.LogWarning(ex,
                    "Outbox message {MessageId} failed. Retry {RetryCount}/{MaxRetries}. Next retry: {NextRetry}",
                    message.Id, message.RetryCount, message.MaxRetries, message.NextRetryAtUtc);

                if (message.IsFailed)
                {
                    _logger.LogError(
                        "Outbox message {MessageId} permanently failed after {MaxRetries} retries. MANUAL INTERVENTION REQUIRED.",
                        message.Id, message.MaxRetries);
                }
            }

            await outboxRepository.UpdateAsync(message, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(
        IServiceProvider serviceProvider,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        switch (message.Type)
        {
            case OutboxMessageType.ProvisionOrganization:
                await ProcessProvisionOrganizationAsync(serviceProvider, message, cancellationToken);
                break;

            case OutboxMessageType.SendWelcomeEmail:
                await ProcessSendWelcomeEmailAsync(serviceProvider, message, cancellationToken);
                break;

            default:
                _logger.LogWarning("Unknown outbox message type: {Type}", message.Type);
                break;
        }
    }

    private async Task ProcessProvisionOrganizationAsync(
        IServiceProvider serviceProvider,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<ProvisionOrganizationPayload>(message.Payload);
        if (payload == null)
        {
            throw new InvalidOperationException($"Invalid payload for message {message.Id}");
        }

        var provisioningClient = serviceProvider.GetRequiredService<ILocaGuestProvisioningClient>();
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var dbContext = serviceProvider.GetRequiredService<AuthDbContext>();
        var configuration = serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var emailing = serviceProvider.GetRequiredService<IEmailingService>();

        // 1. Call LocaGuest API to create Organization
        var orgRequest = new ProvisionOrganizationRequest
        {
            OrganizationName = payload.OrganizationName,
            OrganizationEmail = payload.Email,
            OrganizationPhone = payload.Phone,
            OwnerUserId = payload.UserId.ToString("D"),
            OwnerEmail = payload.Email
        };

        var provisioned = await provisioningClient.ProvisionOrganizationAsync(orgRequest, cancellationToken);

        if (provisioned == null)
        {
            throw new InvalidOperationException("LocaGuest API returned null for organization provisioning");
        }

        _logger.LogInformation(
            "Organization provisioned: {Code} - {Name} for user {UserId}",
            provisioned.Code, provisioned.Name, payload.UserId);

        // 2. Update User with OrganizationId and set status to Active
        var user = await userManager.FindByIdAsync(payload.UserId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User {payload.UserId} not found");
        }

        user.OrganizationId = provisioned.OrganizationId;
        user.Status = UserStatus.Active;
        user.IsActive = true;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update user: {errors}");
        }

        _logger.LogInformation(
            "User {UserId} activated with organization {OrganizationId}",
            user.Id, provisioned.OrganizationId);

        var confirmToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var frontendUrl = configuration["Frontend:ConfirmEmailUrl"] ?? "http://localhost:4200/confirm-email";
        var verifyUrl = $"{frontendUrl}?token={Uri.EscapeDataString(confirmToken)}&email={Uri.EscapeDataString(user.Email!)}";

        var firstName = user.FirstName ?? string.Empty;
        var subject = "Vérifiez votre adresse email";
        var htmlBody = $$"""
<h2>✉️ Vérification d'email</h2>
<p>Bonjour {{firstName}},</p>
<p>Pour finaliser votre inscription, veuillez vérifier votre adresse email en cliquant sur le bouton ci-dessous :</p>
<p><a href="{{verifyUrl}}">Vérifier mon email</a></p>
<p>Si vous n'êtes pas à l'origine de cette demande, ignorez cet email.</p>
""";

        await emailing.QueueHtmlAsync(
            toEmail: user.Email!,
            subject: subject,
            htmlContent: htmlBody,
            textContent: null,
            attachments: null,
            tags: EmailUseCaseTags.AuthConfirmEmail,
            cancellationToken: cancellationToken);
    }

    private Task ProcessSendWelcomeEmailAsync(
        IServiceProvider serviceProvider,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        // TODO: Implement welcome email sending
        _logger.LogInformation("SendWelcomeEmail not yet implemented for message {MessageId}", message.Id);
        return Task.CompletedTask;
    }
}
