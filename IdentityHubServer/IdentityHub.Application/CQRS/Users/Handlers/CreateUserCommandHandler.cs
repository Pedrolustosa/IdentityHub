using IdentityHub.Application.CQRS.Users.Commands;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Users.Handlers;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand>
{
    private readonly IUserRepository _repository;

    public CreateUserCommandHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var email = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _repository.GetByEmailAsync(email, cancellationToken);

        if (existingUser is not null)
            throw new InvalidOperationException("User already exists");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = request.FullName?.Trim(),
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateAsync(user, request.Password, cancellationToken);
    }
}