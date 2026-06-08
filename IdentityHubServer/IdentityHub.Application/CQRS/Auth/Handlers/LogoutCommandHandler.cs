using System;
using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Auth.Handlers;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IAuthRepository _repo;

    public LogoutCommandHandler(IAuthRepository repo)
    {
        _repo = repo;
    }

    public async Task<Result> Handle(LogoutCommand cmd, CancellationToken ct)
    {
        var token = await _repo.GetRefreshTokenAsync(cmd.Request.RefreshToken, ct);

        if (token != null)
        {
            if (!string.Equals(token.UserId, cmd.UserId, StringComparison.Ordinal))
                return Result.Failure(Error.Create("Auth.Forbidden", "Refresh token does not belong to the authenticated user"));

            await _repo.RevokeRefreshTokenAsync(token, ct);

            var session = await _repo.GetSessionByIdAsync(token.SessionId, ct);
            if (session != null && session.IsActive)
                await _repo.RevokeSessionAsync(session, ct);
        }

        await _repo.SaveChangesAsync(ct);

        return Result.Success();
    }
}