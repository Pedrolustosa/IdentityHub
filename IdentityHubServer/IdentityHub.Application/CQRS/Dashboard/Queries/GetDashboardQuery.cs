using IdentityHub.Application.DTOs;
using MediatR;

namespace IdentityHub.Application.CQRS.Dashboard.Queries;

public sealed record GetDashboardQuery : IRequest<DashboardResponse>;