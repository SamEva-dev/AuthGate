using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuthGate.Auth.Application.Common.Clients;
using AuthGate.Auth.Application.Common.Clients.Models;
using AuthGate.Auth.Application.Common.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Infrastructure.Services;

public sealed class LocaGuestInvitationProvisioningClient : ILocaGuestInvitationProvisioningClient
{
    private readonly HttpClient _http;
    private readonly IMachineTokenProvider _machineTokenProvider;
    private readonly ILogger<LocaGuestInvitationProvisioningClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocaGuestInvitationProvisioningClient(
        HttpClient http,
        IMachineTokenProvider machineTokenProvider,
        ILogger<LocaGuestInvitationProvisioningClient> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _http = http;
        _machineTokenProvider = machineTokenProvider;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ConsumeInvitationResponse?> ConsumeInvitationAsync(
        ConsumeInvitationRequest request,
        CancellationToken ct = default)
    {
        var token = await _machineTokenProvider.GetProvisioningTokenAsync(ct);
        var idempotencyKey = Guid.NewGuid().ToString("D");

        using var msg = new HttpRequestMessage(HttpMethod.Post, "api/provisioning/invitations/consume");
        PropagateCorrelationId(msg);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        msg.Headers.Add("Idempotency-Key", idempotencyKey);
        msg.Content = JsonContent.Create(request);

        using var res = await _http.SendAsync(msg, ct);

        if (res.IsSuccessStatusCode)
            return await res.Content.ReadFromJsonAsync<ConsumeInvitationResponse>(cancellationToken: ct);

        if (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            _logger.LogError("Consume invitation auth failure ({Status}): {Body}", (int)res.StatusCode, body);
            return null;
        }

        var error = await res.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException($"Consume invitation failed. Status={(int)res.StatusCode}. Body={error}");
    }

    private void PropagateCorrelationId(HttpRequestMessage msg)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(correlationId) && !msg.Headers.Contains("X-Correlation-Id"))
        {
            msg.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        }
    }
}
