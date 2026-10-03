using IdentityHub.Application.Common.Results;
using MediatR;

namespace IdentityHub.Application.CQRS.Sessions.Commands;

public sealed record RevokeSystemSessionCommand(Guid SessionId) : IRequest<Result>;
