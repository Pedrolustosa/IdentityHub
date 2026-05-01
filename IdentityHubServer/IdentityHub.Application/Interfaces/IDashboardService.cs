using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;

namespace IdentityHub.Application.Interfaces;

public interface IDashboardService
{
    Task<Result<DashboardResponse>> GetAsync(CancellationToken cancellationToken);
}