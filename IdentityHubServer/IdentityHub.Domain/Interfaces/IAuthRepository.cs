using IdentityHub.Domain.Entities;

namespace IdentityHub.Domain.Interfaces
{
    public interface IAuthRepository
    {
        Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default);
        Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default);
        Task<List<RefreshToken>> GetActiveRefreshTokensAsync(string userId, CancellationToken cancellationToken = default);
        Task<List<RefreshToken>> GetActiveRefreshTokensBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
        Task RevokeRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default);

        Task AddSessionAsync(UserSession session, CancellationToken cancellationToken = default);
        Task<List<UserSession>> GetActiveSessionsAsync(string userId, CancellationToken cancellationToken = default);
        Task<UserSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
        Task RevokeSessionAsync(UserSession session, CancellationToken cancellationToken = default);

        Task AddSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}