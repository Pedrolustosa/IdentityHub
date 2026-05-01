using MediatR;

namespace IdentityHub.Application.CQRS.RoleClaims.Queries;

public sealed record GetRoleClaimPermissionsQuery(string RoleId) : IRequest<List<string>>;