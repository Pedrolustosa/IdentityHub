using IdentityHub.Application.Common.Results;
using MediatR;

namespace IdentityHub.Application.CQRS.RoleClaims.Queries;

public sealed record GetRoleClaimPermissionsQuery(string RoleId)
    : IRequest<Result<List<string>>>;