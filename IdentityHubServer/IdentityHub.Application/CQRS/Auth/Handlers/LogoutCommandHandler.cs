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
            await _repo.RevokeRefreshTokenAsync(token, ct);

        await _repo.SaveChangesAsync(ct);

        return Result.Success();
    }
}