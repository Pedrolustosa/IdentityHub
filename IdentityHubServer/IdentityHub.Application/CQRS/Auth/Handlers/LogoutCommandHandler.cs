using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Auth.Handlers;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IAuthRepository _authRepository;

    public LogoutCommandHandler(IAuthRepository authRepository)
    {
        _authRepository = authRepository;
    }

    public async Task Handle(
        LogoutCommand command,
        CancellationToken cancellationToken)
    {
        var token = await _authRepository.GetRefreshTokenAsync(
            command.Request.RefreshToken,
            cancellationToken);

        if (token == null)
            return;

        await _authRepository.RevokeRefreshTokenAsync(token, cancellationToken);

        var sessions = await _authRepository.GetActiveSessionsAsync(
            token.UserId,
            cancellationToken);

        foreach (var session in sessions)
            await _authRepository.RevokeSessionAsync(session, cancellationToken);

        await _authRepository.SaveChangesAsync(cancellationToken);
    }
}