using IdentityHub.Application.CQRS.Roles.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Roles.Handlers;

public sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, List<RoleResponse>>
{
    private readonly IRoleRepository _repository;

    public GetRolesQueryHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<RoleResponse>> Handle(
        GetRolesQuery request,
        CancellationToken cancellationToken)
    {
        var roles = await _repository.GetAllAsync(cancellationToken);

        return roles.Select(role => new RoleResponse
        {
            Id = role.Id,
            Name = role.Name
        }).ToList();
    }
}