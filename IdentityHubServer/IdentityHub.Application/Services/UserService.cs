using IdentityHub.Application.Common.Results;
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

    public Task<Result<List<UserResponse>>> GetAllAsync(CancellationToken cancellationToken)
        => _sender.Send(new GetUsersQuery(), cancellationToken);

    public Task<Result<UserResponse>> GetByIdAsync(string id, CancellationToken cancellationToken)
        => _sender.Send(new GetUserByIdQuery(id), cancellationToken);

    public Task<Result> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
        => _sender.Send(new CreateUserCommand(request), cancellationToken);

    public Task<Result> InviteAsync(InviteUserRequest request, CancellationToken cancellationToken)
        => _sender.Send(new InviteUserCommand(request), cancellationToken);

    public Task<Result> UpdateAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken)
        => _sender.Send(new UpdateUserCommand(id, request), cancellationToken);

    public Task<Result> DeleteAsync(string id, CancellationToken cancellationToken)
        => _sender.Send(new DeleteUserCommand(id), cancellationToken);

    public Task<Result> UpdateRolesAsync(string id, UpdateRolesRequest request, CancellationToken cancellationToken)
        => _sender.Send(new UpdateUserRolesCommand(id, request), cancellationToken);
}