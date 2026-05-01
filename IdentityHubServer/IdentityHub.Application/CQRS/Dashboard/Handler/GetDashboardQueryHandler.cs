using IdentityHub.Application.CQRS.Dashboard.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Dashboard.Handlers;

public sealed class GetDashboardQueryHandler
    : IRequestHandler<GetDashboardQuery, DashboardResponse>
{
    private readonly IDashboardRepository _repository;

    public GetDashboardQueryHandler(IDashboardRepository repository)
    {
        _repository = repository;
    }

    public async Task<DashboardResponse> Handle(
        GetDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var totalUsers = await _repository.GetTotalUsersAsync(cancellationToken);
        var activeSessions = await _repository.GetActiveSessionsAsync(cancellationToken);
        var newUsers = await _repository.GetNewUsersThisWeekAsync(cancellationToken);
        var securityEvents = await _repository.GetSecurityEventsThisWeekAsync(cancellationToken);

        var lastUsers = await _repository.GetUsersLastWeekAsync(cancellationToken);
        var lastSessions = await _repository.GetSessionsLastWeekAsync(cancellationToken);
        var lastSecurity = await _repository.GetSecurityEventsLastWeekAsync(cancellationToken);

        return new DashboardResponse
        {
            TotalUsers = totalUsers,
            ActiveSessions = activeSessions,
            NewUsers = newUsers,
            SecurityEvents = securityEvents,

            UsersGrowth = CalculateGrowth(lastUsers, newUsers),
            SessionsGrowth = CalculateGrowth(lastSessions, activeSessions),
            SecurityGrowth = CalculateGrowth(lastSecurity, securityEvents)
        };
    }

    private static double CalculateGrowth(int previous, int current)
    {
        if (previous == 0)
            return current > 0 ? 100 : 0;

        return Math.Round(((double)(current - previous) / previous) * 100, 2);
    }
}