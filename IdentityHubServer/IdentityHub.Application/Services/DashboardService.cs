using IdentityHub.Application.CQRS.Dashboard.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using MediatR;

namespace IdentityHub.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly ISender _sender;

    public DashboardService(ISender sender)
    {
        _sender = sender;
    }

    public Task<DashboardResponse> GetAsync(CancellationToken cancellationToken = default)
        => _sender.Send(new GetDashboardQuery(), cancellationToken);
}