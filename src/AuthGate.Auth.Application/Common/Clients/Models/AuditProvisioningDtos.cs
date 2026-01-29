using System;
using System.Collections.Generic;

namespace AuthGate.Auth.Application.Common.Clients.Models;

public sealed record LocaGuestAuditLogDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string? UserEmail { get; init; }
    public Guid? OrganizationId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string? EntityId { get; init; }
    public DateTime Timestamp { get; init; }
    public string IpAddress { get; init; } = string.Empty;
    public string? UserAgent { get; init; }
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? Changes { get; init; }
    public string? RequestPath { get; init; }
    public string? HttpMethod { get; init; }
    public int? StatusCode { get; init; }
    public long? DurationMs { get; init; }
    public string? CorrelationId { get; init; }
    public string? SessionId { get; init; }
    public string? AdditionalData { get; init; }
}

public sealed record LocaGuestPagedResultDto<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
