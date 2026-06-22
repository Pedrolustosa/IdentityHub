using IdentityHub.Application.Common.Results;
using MediatR;

namespace IdentityHub.Application.CQRS.UserInvites.Commands;

public sealed record CancelUserInviteCommand(Guid InviteId) : IRequest<Result>;
