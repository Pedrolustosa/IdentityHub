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

        public Task<List<UserSession>> GetRecentSessionsAsync(
            string userId,
            int take,
            CancellationToken cancellationToken = default)
        {
            var safeTake = Math.Clamp(take, 1, 100);

            return _context.UserSessions
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(safeTake)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyList<UserSessionListItem> Items, int TotalCount)> GetPagedSessionsAsync(
            SessionFilter filter,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query =
                from session in _context.UserSessions.AsNoTracking()
                join user in _context.Users.IgnoreQueryFilters().AsNoTracking()
                    on session.UserId equals user.Id into users
                from user in users.DefaultIfEmpty()
                select new { session, user };

            if (filter.ActiveOnly)
                query = query.Where(x => x.session.IsActive);

            if (!string.IsNullOrWhiteSpace(filter.UserId))
            {
                var userId = filter.UserId.Trim();
                query = query.Where(x => x.session.UserId == userId);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim().ToLowerInvariant();
                query = query.Where(x =>
                    (x.user != null && x.user.Email != null && x.user.Email.ToLower().Contains(search)) ||
                    (x.user != null && x.user.FullName != null && x.user.FullName.ToLower().Contains(search)) ||
                    x.session.IpAddress.ToLower().Contains(search) ||
                    x.session.Browser.ToLower().Contains(search) ||
                    x.session.OperatingSystem.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var pageRows = await query
                .OrderByDescending(x => x.session.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.session.Id,
                    x.session.UserId,
                    x.session.IpAddress,
                    x.session.Browser,
                    x.session.OperatingSystem,
                    x.session.CreatedAt,
                    x.session.LastAccessAt,
                    x.session.RevokedAt,
                    x.session.IsActive,
                    Email = x.user != null ? x.user.Email : null,
                    FullName = x.user != null ? x.user.FullName : null
                })
                .ToListAsync(cancellationToken);

            var items = pageRows.Select(x => new UserSessionListItem
            {
                Session = new UserSession
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    IpAddress = x.IpAddress,
                    Browser = x.Browser,
                    OperatingSystem = x.OperatingSystem,
                    CreatedAt = x.CreatedAt,
                    LastAccessAt = x.LastAccessAt,
                    RevokedAt = x.RevokedAt,
                    IsActive = x.IsActive
                },
                Email = x.Email,
                FullName = x.FullName
            }).ToList();

            return (items, totalCount);
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