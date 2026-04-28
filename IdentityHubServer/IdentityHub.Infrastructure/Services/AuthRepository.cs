using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Infrastructure.Services
{
    public class AuthRepository : IAuthRepository
    {
        private readonly AppDbContext _context;

        public AuthRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task SaveRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _context.RefreshTokens.Add(token);
            return Task.CompletedTask;
        }

        public Task RevokeRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            token.IsRevoked = true;
            return Task.CompletedTask;
        }

        public Task CreateSessionAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _context.UserSessions.Add(session);
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            return _context.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
        }

        public Task<List<UserSession>> GetActiveSessionsAsync(string userId, CancellationToken cancellationToken = default)
        {
            return _context.UserSessions
                .Where(x => x.UserId == userId && x.IsActive)
                .ToListAsync(cancellationToken);
        }

        public Task<List<RefreshToken>> GetActiveRefreshTokensAsync(string userId, CancellationToken cancellationToken = default)
        {
            return _context.RefreshTokens
                .Where(x => x.UserId == userId && !x.IsRevoked)
                .ToListAsync(cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
