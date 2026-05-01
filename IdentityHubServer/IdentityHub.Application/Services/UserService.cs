using IdentityHub.Application.CQRS.Users.Commands;
using IdentityHub.Application.CQRS.Users.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using MediatR;

namespace IdentityHub.Application.Services;

public sealed class UserService : IUserService
{
    private readonly ISender _sender;

    public UserService(ISender sender)
    {
        _sender = sender;
    }

    public Task<List<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        => _sender.Send(new GetUsersQuery(), cancellationToken);

    public Task<UserResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => _sender.Send(new GetUserByIdQuery(id), cancellationToken);

    public Task CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        => _sender.Send(new CreateUserCommand(request), cancellationToken);

    public Task UpdateAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken = default)
        => _sender.Send(new UpdateUserCommand(id, request), cancellationToken);

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        => _sender.Send(new DeleteUserCommand(id), cancellationToken);

    public Task UpdateRolesAsync(string id, UpdateRolesRequest request, CancellationToken cancellationToken = default)
        => _sender.Send(new UpdateUserRolesCommand(id, request), cancellationToken);
}