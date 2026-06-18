using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Users.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Users.Handlers;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserResponse>>
{
    private readonly IUserRepository _repository;

    public GetUserByIdQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<UserResponse>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (user is null)
            return Result<UserResponse>.Failure(
                Error.Create("User.NotFound", "User not found"));

        var roles = await _repository.GetRolesAsync(user, cancellationToken);
        var lastLoginAt = await _repository.GetLastLoginAtAsync(user.Id, cancellationToken);
        var activeSessions = await _repository.GetActiveSessionsCountAsync(user.Id, cancellationToken);

        return Result<UserResponse>.Success(new UserResponse
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
}