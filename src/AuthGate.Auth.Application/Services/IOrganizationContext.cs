using System;

namespace AuthGate.Auth.Application.Services;

public interface IOrganizationContext
{
    Guid? OrganizationId { get; }
    string? OrganizationCode { get; }
    string? OrganizationName { get; }
    bool IsAuthenticated { get; }
}
