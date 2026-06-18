using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Users.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Users.Handlers;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<List<UserResponse>>>
{
    private readonly IUserRepository _repository;

    public GetUsersQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<UserResponse>>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await _repository.GetAllAsync(cancellationToken);

        var response = new List<UserResponse>();

        foreach (var user in users)
        {
            var roles = await _repository.GetRolesAsync(user, cancellationToken);
            var lastLoginAt = await _repository.GetLastLoginAtAsync(user.Id, cancellationToken);
            var activeSessions = await _repository.GetActiveSessionsCountAsync(user.Id, cancellationToken);

            response.Add(new UserResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                LastLoginAt = lastLoginAt,
                ActiveSessions = activeSessions,
                Roles = roles
            });
        }

        return Result<List<UserResponse>>.Success(response);
    }
}