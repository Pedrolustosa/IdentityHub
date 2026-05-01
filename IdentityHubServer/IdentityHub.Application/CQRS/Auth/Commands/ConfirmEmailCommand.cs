using IdentityHub.Application.Common.Results;
using MediatR;

namespace IdentityHub.Application.CQRS.Auth.Commands;

public sealed record ConfirmEmailCommand(
    string Email,
    string Token) : IRequest<Result>;