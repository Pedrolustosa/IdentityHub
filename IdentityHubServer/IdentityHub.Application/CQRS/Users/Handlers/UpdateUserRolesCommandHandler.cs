using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Users.Commands;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Users.Handlers;

public sealed class UpdateUserRolesCommandHandler : IRequestHandler<UpdateUserRolesCommand, Result>
{
    private readonly IUserRepository _repository;
    private readonly IAuditLogRepository _auditLogRepository;

    public UpdateUserRolesCommandHandler(
        IUserRepository repository,
        IAuditLogRepository auditLogRepository)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result> Handle(
        UpdateUserRolesCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(command.Id, cancellationToken);

        if (user is null)
            return Result.Failure(
                Error.Create("User.NotFound", "User not found"));

        if (command.Request.Roles is null || !command.Request.Roles.Any())
            return Result.Failure(
                Error.Create("User.RolesRequired", "At least one role is required"));

        await _repository.UpdateRolesAsync(
            user,
            command.Request.Roles,
            cancellationToken);

        var assignedRoles = string.Join(",", command.Request.Roles.Select(r => r.Trim()));

        await _auditLogRepository.WriteAsync(
            "Audit.User.RolesUpdated",
            $"User roles updated: id={user.Id}, email={user.Email}, roles=[{assignedRoles}]",
            cancellationToken);

        return Result.Success();
    }
}