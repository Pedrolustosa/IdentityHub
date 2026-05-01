using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Roles.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Roles.Handlers;

public sealed class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, Result<RoleResponse>>
{
    private readonly IRoleRepository _repository;

    public GetRoleByIdQueryHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<RoleResponse>> Handle(
        GetRoleByIdQuery request,
        CancellationToken cancellationToken)
    {
        var role = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (role is null)
            return Result<RoleResponse>.Failure(
                Error.Create("Role.NotFound", "Role not found"));

        return Result<RoleResponse>.Success(new RoleResponse
        {
            Id = role.Id,
            Name = role.Name
        });
    }
}