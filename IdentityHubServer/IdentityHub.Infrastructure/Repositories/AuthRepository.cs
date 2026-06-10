using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Infrastructure.Repositories
{
    public sealed class AuthRepository : IAuthRepository
    {
        private readonly AppDbContext _context;

        public AuthRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddRefreshTokenAsync(
            RefreshToken token,
            CancellationToken cancellationToken = default)
        {
            await _context.RefreshTokens.AddAsync(token, cancellationToken);
        }

        public Task<RefreshToken?> GetRefreshTokenAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            return _context.RefreshTokens
                .IgnoreQueryFilters()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        }

        public Task<List<RefreshToken>> GetActiveRefreshTokensAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return _context.RefreshTokens
                .Where(x => x.UserId == userId && !x.IsRevoked)
                .ToListAsync(cancellationToken);
        }

        public Task<List<RefreshToken>> GetActiveRefreshTokensBySessionAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            return _context.RefreshTokens
                .Where(x => x.SessionId == sessionId && !x.IsRevoked)
                .ToListAsync(cancellationToken);
        }

        public Task RevokeRefreshTokenAsync(
            RefreshToken token,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            token.IsRevoked = true;

            return Task.CompletedTask;
        }

        public async Task AddSessionAsync(
            UserSession session,
            CancellationToken cancellationToken = default)
        {
            await _context.UserSessions.AddAsync(session, cancellationToken);
        }

        public Task<List<UserSession>> GetActiveSessionsAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return _context.UserSessions
                .Where(x => x.UserId == userId && x.IsActive)
                .ToListAsync(cancellationToken);
        }

        public Task<UserSession?> GetSessionByIdAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            return _context.UserSessions
                .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        }

        public Task RevokeSessionAsync(
            UserSession session,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            session.IsActive = false;
            session.RevokedAt = DateTime.UtcNow;

            return Task.CompletedTask;
        }

        public async Task AddSecurityEventAsync(
            SecurityEvent securityEvent,
            CancellationToken cancellationToken = default)
        {
            await _context.SecurityEvents.AddAsync(securityEvent, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}