using IdentityHub.Application.CQRS.Roles.Queries;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Roles.Handlers;

public sealed class GetRolePermissionsQueryHandler : IRequestHandler<GetRolePermissionsQuery, List<string>>
{
    private const string PermissionClaimType = "permission";
    private readonly IRoleRepository _repository;

    public GetRolePermissionsQueryHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<string>> Handle(
        GetRolePermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var role = await _repository.GetByIdAsync(request.RoleId, cancellationToken);

        if (role is null)
            throw new InvalidOperationException("Role not found");

        var claims = await _repository.GetClaimsAsync(role, cancellationToken);

        return claims
            .Where(c => c.Type == PermissionClaimType)
            .Select(c => c.Value)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }
}