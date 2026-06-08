using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Auth.Handlers;

public sealed class RevokeSessionCommandHandler : IRequestHandler<RevokeSessionCommand, Result>
{
    private readonly IAuthRepository _repo;

    public RevokeSessionCommandHandler(IAuthRepository repo)
    {
        _repo = repo;
    }

    public async Task<Result> Handle(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await _repo.GetSessionByIdAsync(command.SessionId, cancellationToken);

        if (session is null || !session.IsActive)
            return Result.Failure(Error.Create("Session.NotFound", "Session not found"));

        if (!string.Equals(session.UserId, command.UserId, StringComparison.Ordinal))
            return Result.Failure(Error.Create("Auth.Forbidden", "Session does not belong to the authenticated user"));

        await _repo.RevokeSessionAsync(session, cancellationToken);

        var refreshTokens = await _repo.GetActiveRefreshTokensBySessionAsync(command.SessionId, cancellationToken);
        foreach (var refreshToken in refreshTokens)
            await _repo.RevokeRefreshTokenAsync(refreshToken, cancellationToken);

        await _repo.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}