using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

    public Task<AuditLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.AuditLogEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetRecentByUserAsync(
        string userId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var safeTake = Math.Clamp(take, 1, 100);

        return await _context.AuditLogEntries
            .AsNoTracking()
            .Where(x => x.ActorUserId == userId || x.TargetId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(safeTake)
            .ToListAsync(cancellationToken);
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

    public Task WriteAsync(
        string eventType,
        string description,
        CancellationToken cancellationToken = default)
        => WriteAsync(eventType, description, null, null, cancellationToken);

    public async Task WriteAsync(
        string eventType,
        string description,
        string? targetId,
        object? metadata,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserContext.UserId;

        _context.AuditLogEntries.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = string.IsNullOrWhiteSpace(userId) ? "system" : userId,
            Type = eventType,
            TargetId = string.IsNullOrWhiteSpace(targetId) ? null : targetId,
            Description = description,
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata),
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
