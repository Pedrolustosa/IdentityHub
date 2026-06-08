using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Users.Commands;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Users.Handlers;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result>
{
    private readonly IUserRepository _repository;
    private readonly IAuditLogRepository _auditLogRepository;

    public CreateUserCommandHandler(
        IUserRepository repository,
        IAuditLogRepository auditLogRepository)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result> Handle(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Request.Email) ||
            string.IsNullOrWhiteSpace(command.Request.Password))
        {
            return Result.Failure(
                Error.Create("User.InvalidRequest", "Email and password are required"));
        }

        var email = command.Request.Email.Trim().ToLowerInvariant();

        var existingUser = await _repository.GetByEmailAsync(email, cancellationToken);

        if (existingUser is not null)
            return Result.Failure(
                Error.Create("User.AlreadyExists", "User already exists"));

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = command.Request.FullName?.Trim(),
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateAsync(
            user,
            command.Request.Password,
            cancellationToken);

        await _auditLogRepository.WriteAsync(
            "Audit.User.Created",
            $"User created: id={user.Id}, email={email}",
            cancellationToken);

        return Result.Success();
    }
}