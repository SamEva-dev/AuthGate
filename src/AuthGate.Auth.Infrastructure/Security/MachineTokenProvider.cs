using System;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.Common.Security;
using AuthGate.Auth.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace AuthGate.Auth.Infrastructure.Security;

public sealed class MachineTokenProvider : IMachineTokenProvider
{
    private readonly IJwtService _jwtService;
    private readonly IOptionsMonitor<MachineTokenOptions> _options;

    private readonly object _lock = new();
    private string? _cachedToken;
    private DateTime _cachedTokenExpUtc;

    public MachineTokenProvider(IJwtService jwtService, IOptionsMonitor<MachineTokenOptions> options)
    {
        _jwtService = jwtService;
        _options = options;
    }

    public Task<string> GetProvisioningTokenAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        lock (_lock)
        {
            if (_cachedToken is not null && now < _cachedTokenExpUtc.AddSeconds(-15))
                return Task.FromResult(_cachedToken);

            var opt = _options.CurrentValue;
            var lifetime = TimeSpan.FromMinutes(opt.TokenLifetimeMinutes);
            var exp = now.Add(lifetime);

            var token = _jwtService.GenerateMachineToken(
                scope: "locaguest.provisioning",
                clientId: opt.ClientId,
                lifetime: lifetime,
                audience: opt.Audience);

            _cachedToken = token;
            _cachedTokenExpUtc = exp;

            return Task.FromResult(token);
        }
    }
}
