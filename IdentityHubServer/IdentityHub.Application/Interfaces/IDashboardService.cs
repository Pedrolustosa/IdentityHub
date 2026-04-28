using IdentityHub.Application.DTOs;

namespace IdentityHub.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardResponse> GetAsync(CancellationToken cancellationToken = default);
    }
}
