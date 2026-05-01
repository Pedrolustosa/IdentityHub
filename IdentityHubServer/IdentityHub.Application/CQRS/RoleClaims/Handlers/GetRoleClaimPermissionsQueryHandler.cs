using IdentityHub.Application.CQRS.RoleClaims.Queries;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.RoleClaims.Handlers;

public sealed class GetRoleClaimPermissionsQueryHandler
    : IRequestHandler<GetRoleClaimPermissionsQuery, List<string>>
{
    private const string PermissionClaimType = "permission";
    private readonly IRoleRepository _repository;

    public GetRoleClaimPermissionsQueryHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<string>> Handle(
        GetRoleClaimPermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var role = await _repository.GetByIdAsync(request.RoleId, cancellationToken);

        if (role is null)
            throw new InvalidOperationException("Role not found");

        var claims = await _repository.GetClaimsAsync(role, cancellationToken);

        return claims
            .Where(c => c.Type == PermissionClaimType)
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }
}