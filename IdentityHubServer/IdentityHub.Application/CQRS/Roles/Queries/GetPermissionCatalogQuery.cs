using IdentityHub.Application.Common.Results;
using MediatR;

namespace IdentityHub.Application.CQRS.Roles.Queries;

public sealed record GetPermissionCatalogQuery : IRequest<Result<List<string>>>;
