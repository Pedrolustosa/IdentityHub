using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IdentityHub.Application.CQRS.Auth.Handlers;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthRepository _authRepository;

    public ChangePasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IAuthRepository authRepository)
    {
        _userManager = userManager;
        _authRepository = authRepository;
    }

    public async Task Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(command.UserId);

        if (user is null)
            throw new UnauthorizedAccessException();

        var result = await _userManager.ChangePasswordAsync(
            user,
            command.Request.CurrentPassword,
            command.Request.NewPassword);

        if (!result.Succeeded)
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(e => e.Description)));

        await RevokeUserAccess(user.Id, cancellationToken);
    }

    private async Task RevokeUserAccess(string userId, CancellationToken cancellationToken)
    {
        var sessions = await _authRepository.GetActiveSessionsAsync(userId, cancellationToken);

        foreach (var session in sessions)
            await _authRepository.RevokeSessionAsync(session, cancellationToken);

        var tokens = await _authRepository.GetActiveRefreshTokensAsync(userId, cancellationToken);

        foreach (var token in tokens)
            await _authRepository.RevokeRefreshTokenAsync(token, cancellationToken);

        await _authRepository.SaveChangesAsync(cancellationToken);
    }
}