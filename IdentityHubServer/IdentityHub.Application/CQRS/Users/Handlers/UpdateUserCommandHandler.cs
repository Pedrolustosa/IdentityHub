using IdentityHub.Application.CQRS.Users.Commands;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Users.Handlers;

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand>
{
    private readonly IUserRepository _repository;

    public UpdateUserCommandHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(command.Id, cancellationToken);

        if (user is null)
            throw new InvalidOperationException("User not found");

        user.FullName = command.Request.FullName?.Trim();
        user.IsActive = command.Request.IsActive;

        await _repository.UpdateAsync(user, cancellationToken);
    }
}