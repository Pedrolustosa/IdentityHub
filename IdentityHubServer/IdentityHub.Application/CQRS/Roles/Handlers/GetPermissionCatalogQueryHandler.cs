using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Roles.Queries;
using IdentityHub.Domain.Constants;
using MediatR;

namespace IdentityHub.Application.CQRS.Roles.Handlers;

public sealed class GetPermissionCatalogQueryHandler
    : IRequestHandler<GetPermissionCatalogQuery, Result<List<string>>>
{
    public Task<Result<List<string>>> Handle(
        GetPermissionCatalogQuery request,
        CancellationToken cancellationToken)
    {
        var catalog = AppPermissions.All()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        return Task.FromResult(Result<List<string>>.Success(catalog));
    }
}
