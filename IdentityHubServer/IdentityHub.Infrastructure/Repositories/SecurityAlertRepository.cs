using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Infrastructure.Repositories;

public sealed class SecurityAlertRepository : ISecurityAlertRepository
{
    private readonly AppDbContext _context;

    public SecurityAlertRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<SecurityEvent> Items, int TotalCount)> GetPagedAsync(
        SecurityAlertFilter request,
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

    public Task<SecurityEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.SecurityEvents.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
    {
        _context.SecurityEvents.Update(securityEvent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<SecurityEvent> ApplyFilters(SecurityAlertFilter request)
    {
        var query = _context.SecurityEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var type = request.Type.Trim();
            query = query.Where(x => x.Type.Contains(type));
        }

        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            var userId = request.UserId.Trim();
            query = query.Where(x => x.UserId.Contains(userId));
        }

        if (!string.IsNullOrWhiteSpace(request.Severity))
        {
            var severity = request.Severity.Trim();
            query = query.Where(x => x.Severity == severity);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            query = query.Where(x => x.Status == status);
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
}