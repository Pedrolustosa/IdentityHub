using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.RoleClaims.Queries;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.RoleClaims.Handlers;

public sealed class GetRoleClaimPermissionsQueryHandler
    : IRequestHandler<GetRoleClaimPermissionsQuery, Result<List<string>>>
{
    private const string PermissionClaimType = "permission";
    private readonly IRoleRepository _repository;

    public GetRoleClaimPermissionsQueryHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<string>>> Handle(
        GetRoleClaimPermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var role = await _repository.GetByIdAsync(request.RoleId, cancellationToken);

        if (role is null)
            return Result<List<string>>.Failure(
                Error.Create("Role.NotFound", "Role not found"));

        var claims = await _repository.GetClaimsAsync(role, cancellationToken);

        var permissions = claims
            .Where(c => c.Type == PermissionClaimType)
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        return Result<List<string>>.Success(permissions);
    }
}