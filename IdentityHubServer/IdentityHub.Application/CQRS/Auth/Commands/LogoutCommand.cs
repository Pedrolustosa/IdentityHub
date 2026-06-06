using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using MediatR;

namespace IdentityHub.Application.CQRS.Auth.Commands;

public sealed record LogoutCommand(string UserId, RefreshTokenRequest Request) : IRequest<Result>;