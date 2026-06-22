using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using MediatR;

namespace IdentityHub.Application.CQRS.SecuritySettings.Commands;

public sealed record UpdateSecuritySettingsCommand(UpdateSecuritySettingsRequest Request) : IRequest<Result>;
