using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Roles.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Roles.Handlers;

public sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, Result<List<RoleResponse>>>
{
    private readonly IRoleRepository _repository;

    public GetRolesQueryHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<RoleResponse>>> Handle(
        GetRolesQuery request,
        CancellationToken cancellationToken)
    {
        var roles = await _repository.GetAllAsync(cancellationToken);
        var countsByRoleId = await _repository.GetUserCountsByRoleIdAsync(cancellationToken);

        var response = new List<RoleResponse>();
        foreach (var role in roles)
        {
            var userCount = countsByRoleId.TryGetValue(role.Id, out var count) ? count : 0;
            response.Add(new RoleResponse
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                UserCount = userCount
            });
        }

        return Result<List<RoleResponse>>.Success(response);
    }
}