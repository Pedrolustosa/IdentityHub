using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserContext _currentUserContext;

    public AuditLogRepository(AppDbContext context, ICurrentUserContext currentUserContext)
    {
        _context = context;
        _currentUserContext = currentUserContext;
    }

    public async Task<(IReadOnlyList<AuditLogEntry> Items, int TotalCount)> GetPagedAsync(
        AuditLogFilter request,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(request)
            .OrderByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private IQueryable<AuditLogEntry> ApplyFilters(AuditLogFilter request)
    {
        var query = _context.AuditLogEntries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var type = request.Type.Trim();
            query = query.Where(x => x.Type.Contains(type));
        }

        if (!string.IsNullOrWhiteSpace(request.ActorUserId))
        {
            var actorUserId = request.ActorUserId.Trim();
            query = query.Where(x => x.ActorUserId.Contains(actorUserId));
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            var description = request.Description.Trim();
            query = query.Where(x => x.Description.Contains(description));
        }

        if (request.FromDate.HasValue)
        {
            var fromDate = request.FromDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.CreatedAt >= fromDate);
        }

        if (request.ToDate.HasValue)
        {
            var toDateExclusive = request.ToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.CreatedAt < toDateExclusive);
        }

        return query;
    }

    public async Task WriteAsync(
        string eventType,
        string description,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserContext.UserId;

        _context.AuditLogEntries.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = string.IsNullOrWhiteSpace(userId) ? "system" : userId,
            Type = eventType,
            Description = description,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
