using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AuthGate.Auth.Application.Common.Clients;
using AuthGate.Auth.Application.Common.Clients.Models;
using AuthGate.Auth.Application.Common.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Infrastructure.Services;

public sealed class LocaGuestProvisioningClient : ILocaGuestProvisioningClient
{
    private readonly HttpClient _http;
    private readonly IMachineTokenProvider _machineTokenProvider;
    private readonly ILogger<LocaGuestProvisioningClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocaGuestProvisioningClient(
        HttpClient http,
        IMachineTokenProvider machineTokenProvider,
        ILogger<LocaGuestProvisioningClient> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _http = http;
        _machineTokenProvider = machineTokenProvider;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<LocaGuestOrganizationDetailsDto?> GetOrganizationByIdAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        if (organizationId == Guid.Empty)
            return null;

        var token = await _machineTokenProvider.GetProvisioningTokenAsync(ct);

        using var msg = new HttpRequestMessage(HttpMethod.Get, $"api/provisioning/organizations/{organizationId:D}");
        PropagateCorrelationId(msg);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var res = await _http.SendAsync(msg, ct);

        if (res.IsSuccessStatusCode)
        {
            var dto = await res.Content.ReadFromJsonAsync<LocaGuestOrganizationDetailsDto>(cancellationToken: ct);
            return dto;
        }

        if (res.StatusCode is HttpStatusCode.NotFound)
            return null;

        if (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            _logger.LogError("Get organization auth failure ({Status}): {Body}", (int)res.StatusCode, body);
            return null;
        }

        var error = await res.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException($"Get organization failed. Status={(int)res.StatusCode}. Body={error}");
    }

    public async Task<IReadOnlyList<LocaGuestOrganizationListItemDto>> GetOrganizationsAsync(CancellationToken ct = default)
    {
        var token = await _machineTokenProvider.GetProvisioningTokenAsync(ct);

        using var msg = new HttpRequestMessage(HttpMethod.Get, "api/provisioning/organizations");
        PropagateCorrelationId(msg);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var res = await _http.SendAsync(msg, ct);

        if (res.IsSuccessStatusCode)
        {
            var items = await res.Content.ReadFromJsonAsync<List<LocaGuestOrganizationListItemDto>>(cancellationToken: ct);
            return items ?? new List<LocaGuestOrganizationListItemDto>();
        }

        if (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            _logger.LogError("Get organizations auth failure ({Status}): {Body}", (int)res.StatusCode, body);
            return Array.Empty<LocaGuestOrganizationListItemDto>();
        }

        var error = await res.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException($"Get organizations failed. Status={(int)res.StatusCode}. Body={error}");
    }

    public async Task<ProvisionOrganizationResponse?> ProvisionOrganizationAsync(
        ProvisionOrganizationRequest request,
        CancellationToken ct = default)
    {
        var idempotencyKey = Guid.NewGuid().ToString("D");
        var token = await _machineTokenProvider.GetProvisioningTokenAsync(ct);

        var primary = await SendProvisioningAsync(
            path: "api/provisioning/organizations",
            request: request,
            bearerToken: token,
            idempotencyKey: idempotencyKey,
            ct: ct);

        if (primary.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _logger.LogWarning(
                "Provisioning endpoint unavailable/auth denied ({Status}). Falling back to legacy /api/organizations.",
                (int)primary.StatusCode);

            primary.Dispose();

            // Legacy payload differs from provisioning payload; reuse of the same idempotency key would
            // trigger an idempotency conflict (409) server-side.
            var legacyIdempotencyKey = Guid.NewGuid().ToString("D");

            var legacy = await SendLegacyAsync(
                path: "api/organizations",
                request: request,
                bearerToken: token,
                idempotencyKey: legacyIdempotencyKey,
                ct: ct);

            return await HandleLegacyResponseAsync(legacy, ct);
        }

        return await HandleProvisioningResponseAsync(primary, ct);
    }

    public async Task<bool> HardDeleteOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        var token = await _machineTokenProvider.GetProvisioningTokenAsync(ct);

        var primary = await SendDeleteAsync(
            path: $"api/provisioning/organizations/{organizationId:D}/permanent",
            bearerToken: token,
            ct: ct);

        if (primary.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _logger.LogWarning(
                "Provisioning delete endpoint unavailable/auth denied ({Status}). Falling back to legacy /api/organizations/{OrgId}.",
                (int)primary.StatusCode,
                organizationId);

            primary.Dispose();

            var legacy = await SendDeleteAsync(
                path: $"api/organizations/{organizationId:D}",
                bearerToken: token,
                ct: ct);

            return legacy.IsSuccessStatusCode;
        }

        return primary.IsSuccessStatusCode;
    }

    private async Task<HttpResponseMessage> SendProvisioningAsync(
        string path,
        ProvisionOrganizationRequest request,
        string bearerToken,
        string idempotencyKey,
        CancellationToken ct)
    {
        using var msg = new HttpRequestMessage(HttpMethod.Post, path);
        PropagateCorrelationId(msg);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        msg.Headers.Add("Idempotency-Key", idempotencyKey);
        msg.Content = JsonContent.Create(request);

        return await _http.SendAsync(msg, ct);
    }

    private void PropagateCorrelationId(HttpRequestMessage msg)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(correlationId) && !msg.Headers.Contains("X-Correlation-Id"))
        {
            msg.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        }
    }

    private async Task<HttpResponseMessage> SendDeleteAsync(
        string path,
        string bearerToken,
        CancellationToken ct)
    {
        using var msg = new HttpRequestMessage(HttpMethod.Delete, path);
        PropagateCorrelationId(msg);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        return await _http.SendAsync(msg, ct);
    }

    private async Task<HttpResponseMessage> SendLegacyAsync(
        string path,
        ProvisionOrganizationRequest request,
        string bearerToken,
        string idempotencyKey,
        CancellationToken ct)
    {
        using var msg = new HttpRequestMessage(HttpMethod.Post, path);
        PropagateCorrelationId(msg);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        msg.Headers.Add("Idempotency-Key", idempotencyKey);

        var legacyPayload = new
        {
            name = request.OrganizationName,
            email = request.OrganizationEmail,
            phone = request.OrganizationPhone
        };

        msg.Content = JsonContent.Create(legacyPayload);

        return await _http.SendAsync(msg, ct);
    }

    private async Task<ProvisionOrganizationResponse?> HandleProvisioningResponseAsync(HttpResponseMessage res, CancellationToken ct)
    {
        using var _ = res;

        if (res.IsSuccessStatusCode)
            return await res.Content.ReadFromJsonAsync<ProvisionOrganizationResponse>(cancellationToken: ct);

        if (res.StatusCode == HttpStatusCode.Conflict)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Provisioning idempotency conflict (409): {Body}", body);
            return null;
        }

        if (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            _logger.LogError("Provisioning auth failure ({Status}): {Body}", (int)res.StatusCode, body);
            return null;
        }

        var error = await res.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException($"Provisioning failed. Status={(int)res.StatusCode}. Body={error}");
    }

    private async Task<ProvisionOrganizationResponse?> HandleLegacyResponseAsync(HttpResponseMessage res, CancellationToken ct)
    {
        using var _ = res;

        if (!res.IsSuccessStatusCode)
        {
            var error = await res.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Legacy provisioning failed. Status={(int)res.StatusCode}. Body={error}");
        }

        var json = await res.Content.ReadAsStringAsync(ct);

        // legacy endpoint returns Result<CreateOrganizationDto>
        // { isSuccess, data: { organizationId, code, name, email, number }, errorMessage }
        try
        {
            var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var dataEl) || dataEl.ValueKind == JsonValueKind.Null)
                return null;

            return new ProvisionOrganizationResponse
            {
                OrganizationId = dataEl.GetProperty("organizationId").GetGuid(),
                Code = dataEl.GetProperty("code").GetString() ?? string.Empty,
                Name = dataEl.GetProperty("name").GetString() ?? string.Empty,
                Email = dataEl.GetProperty("email").GetString() ?? string.Empty,
                Number = dataEl.TryGetProperty("number", out var n) ? n.GetInt32() : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to parse legacy provisioning response");
            return null;
        }
    }
}
