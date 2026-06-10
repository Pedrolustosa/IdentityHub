using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using MediatR;

namespace IdentityHub.Application.CQRS.Users.Commands;

public sealed record InviteUserCommand(InviteUserRequest Request) : IRequest<Result>;
