using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Dashboard.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Dashboard.Handlers;

public sealed class GetDashboardQueryHandler
    : IRequestHandler<GetDashboardQuery, Result<DashboardResponse>>
{
    private readonly IDashboardRepository _repository;

    public GetDashboardQueryHandler(IDashboardRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<DashboardResponse>> Handle(
        GetDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var totalUsers = await _repository.GetTotalUsersAsync(cancellationToken);
        var activeSessions = await _repository.GetActiveSessionsAsync(cancellationToken);
        var newUsers = await _repository.GetNewUsersThisWeekAsync(cancellationToken);
        var securityEvents = await _repository.GetSecurityEventsThisWeekAsync(cancellationToken);

        var lastWeekUsers = await _repository.GetNewUsersLastWeekAsync(cancellationToken);
        var lastWeekSessions = await _repository.GetSessionsLastWeekAsync(cancellationToken);
        var lastWeekSecurityEvents = await _repository.GetSecurityEventsLastWeekAsync(cancellationToken);

        var response = new DashboardResponse
        {
            TotalUsers = totalUsers,
            ActiveSessions = activeSessions,
            NewUsers = newUsers,
            SecurityEvents = securityEvents,
            UsersGrowth = CalculateGrowth(lastWeekUsers, newUsers),
            SessionsGrowth = CalculateGrowth(lastWeekSessions, activeSessions),
            SecurityGrowth = CalculateGrowth(lastWeekSecurityEvents, securityEvents)
        };

        return Result<DashboardResponse>.Success(response);
    }

    private static double CalculateGrowth(int previous, int current)
    {
        if (previous == 0)
            return current > 0 ? 100 : 0;

        return Math.Round(((double)(current - previous) / previous) * 100, 2);
    }
}