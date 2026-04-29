using IdentityHub.Application.DTOs;
using MediatR;

namespace IdentityHub.Application.CQRS.Auth.Commands;

public sealed record LogoutCommand(RefreshTokenRequest Request) : IRequest;