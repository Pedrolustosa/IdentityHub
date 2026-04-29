using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Domain.Interfaces;
using IdentityHub.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IdentityHub.Application.CQRS.Auth.Handlers;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthRepository _authRepository;

    public ResetPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IAuthRepository authRepository)
    {
        _userManager = userManager;
        _authRepository = authRepository;
    }

    public async Task Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
            throw new InvalidOperationException("Invalid request");

        var decodedToken = Uri.UnescapeDataString(request.Token);

        var result = await _userManager.ResetPasswordAsync(
            user,
            decodedToken,
            request.NewPassword);

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