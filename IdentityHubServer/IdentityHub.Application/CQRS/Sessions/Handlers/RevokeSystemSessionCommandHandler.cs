using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Sessions.Commands;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Sessions.Handlers;

public sealed class RevokeSystemSessionCommandHandler : IRequestHandler<RevokeSystemSessionCommand, Result>
{
    private readonly IAuthRepository _repo;
    private readonly IAuditLogRepository _auditLogRepository;

    public RevokeSystemSessionCommandHandler(
        IAuthRepository repo,
        IAuditLogRepository auditLogRepository)
    {
        _repo = repo;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result> Handle(RevokeSystemSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await _repo.GetSessionByIdAsync(command.SessionId, cancellationToken);

        if (session is null || !session.IsActive)
            return Result.Failure(Error.Create("Session.NotFound", "Session not found"));

        await _repo.RevokeSessionAsync(session, cancellationToken);

        var refreshTokens = await _repo.GetActiveRefreshTokensBySessionAsync(command.SessionId, cancellationToken);
        foreach (var refreshToken in refreshTokens)
            await _repo.RevokeRefreshTokenAsync(refreshToken, cancellationToken);

        await _repo.SaveChangesAsync(cancellationToken);

        await _auditLogRepository.WriteAsync(
            "Audit.Session.Revoked",
            $"Session revoked by admin: sessionId={session.Id}, userId={session.UserId}, revokedTokens={refreshTokens.Count}",
            session.Id.ToString(),
            new { sessionId = session.Id, userId = session.UserId, revokedTokens = refreshTokens.Count, source = "system-sessions" },
            cancellationToken);

        return Result.Success();
    }
}
