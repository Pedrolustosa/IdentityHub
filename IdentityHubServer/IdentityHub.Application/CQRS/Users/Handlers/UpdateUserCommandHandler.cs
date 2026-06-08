using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Users.Commands;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Users.Handlers;

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result>
{
    private readonly IUserRepository _repository;
    private readonly IAuditLogRepository _auditLogRepository;

    public UpdateUserCommandHandler(
        IUserRepository repository,
        IAuditLogRepository auditLogRepository)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result> Handle(
        UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(command.Id, cancellationToken);

        if (user is null)
            return Result.Failure(
                Error.Create("User.NotFound", "User not found"));

        user.FullName = command.Request.FullName?.Trim();
        user.IsActive = command.Request.IsActive;

        await _repository.UpdateAsync(user, cancellationToken);

        await _auditLogRepository.WriteAsync(
            "Audit.User.Updated",
            $"User updated: id={user.Id}, email={user.Email}",
            cancellationToken);

        return Result.Success();
    }
}