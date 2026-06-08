using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Users.Commands;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Users.Handlers;

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result>
{
    private readonly IUserRepository _repository;
    private readonly IAuditLogRepository _auditLogRepository;

    public DeleteUserCommandHandler(
        IUserRepository repository,
        IAuditLogRepository auditLogRepository)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result> Handle(
        DeleteUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(command.Id, cancellationToken);

        if (user is null)
            return Result.Failure(
                Error.Create("User.NotFound", "User not found"));

        var userId = user.Id;
        var userEmail = user.Email ?? string.Empty;

        await _repository.DeleteAsync(user, cancellationToken);

        await _auditLogRepository.WriteAsync(
            "Audit.User.Deleted",
            $"User deleted: id={userId}, email={userEmail}",
            cancellationToken);

        return Result.Success();
    }
}