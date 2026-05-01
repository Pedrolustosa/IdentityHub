using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using MediatR;

namespace IdentityHub.Application.CQRS.Auth.Commands;

public sealed record UpdateProfileCommand(
    string UserId,
    UpdateProfileRequest Request) : IRequest<Result>;