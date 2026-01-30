using AuthGate.Auth.Application.Common;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.ResendConfirmEmail;

public record ResendConfirmEmailCommand : IRequest<Result<bool>>
{
    public required string Email { get; init; }
}
