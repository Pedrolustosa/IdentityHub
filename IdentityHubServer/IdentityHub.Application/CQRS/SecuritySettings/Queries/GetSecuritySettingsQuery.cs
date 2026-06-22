using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using MediatR;

namespace IdentityHub.Application.CQRS.SecuritySettings.Queries;

public sealed record GetSecuritySettingsQuery : IRequest<Result<SecuritySettingsResponse>>;
