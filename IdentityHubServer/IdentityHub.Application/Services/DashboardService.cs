using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Interfaces;

namespace IdentityHub.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _repository;

        public DashboardService(IDashboardRepository repository)
        {
            _repository = repository;
        }

        public async Task<DashboardResponse> GetAsync(CancellationToken cancellationToken = default)
        {
            var totalUsers = await _repository.GetTotalUsersAsync(cancellationToken);
            var activeSessions = await _repository.GetActiveSessionsAsync(cancellationToken);
            var newUsersThisWeek = await _repository.GetNewUsersThisWeekAsync(cancellationToken);
            var newUsersLastWeek = await _repository.GetNewUsersLastWeekAsync(cancellationToken);
            var securityThisWeek = await _repository.GetSecurityEventsThisWeekAsync(cancellationToken);
            var securityLastWeek = await _repository.GetSecurityEventsLastWeekAsync(cancellationToken);

            return new DashboardResponse
            {
                TotalUsers = totalUsers,
                ActiveSessions = activeSessions,
                NewUsers = newUsersThisWeek,
                NewUsersGrowth = CalculateGrowth(newUsersLastWeek, newUsersThisWeek),
                SecurityAlerts = securityThisWeek,
                SecurityGrowth = CalculateGrowth(securityLastWeek, securityThisWeek)
            };
        }

        private double CalculateGrowth(int previous, int current)
        {
            if (previous == 0)
                return current > 0 ? 100 : 0;

            return Math.Round(((double)(current - previous) / previous) * 100, 2);
        }
    }
}
