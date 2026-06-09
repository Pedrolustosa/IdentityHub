using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Users.Commands;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Users.Handlers;

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result>
{
    private readonly IUserRepository _repository;
    private readonly IAuthRepository _authRepository;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogRepository _auditLogRepository;

    public DeleteUserCommandHandler(
        IUserRepository repository,
        IAuthRepository authRepository,
        ICurrentUserContext currentUserContext,
        IAuditLogRepository auditLogRepository)
    {
        _repository = repository;
        _authRepository = authRepository;
        _currentUserContext = currentUserContext;
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
        var deletedBy = _currentUserContext.UserId;

        await _repository.DeleteAsync(user, deletedBy, cancellationToken);

        var activeSessions = await _authRepository.GetActiveSessionsAsync(userId, cancellationToken);
        foreach (var session in activeSessions)
            session.IsActive = false;

        var activeTokens = await _authRepository.GetActiveRefreshTokensAsync(userId, cancellationToken);
        foreach (var token in activeTokens)
            token.IsRevoked = true;

        await _authRepository.SaveChangesAsync(cancellationToken);

        await _auditLogRepository.WriteAsync(
            "Audit.User.Deleted",
            $"User deleted: id={userId}, email={userEmail}, deletedBy={deletedBy ?? "system"}",
            cancellationToken);

        return Result.Success();
    }
}