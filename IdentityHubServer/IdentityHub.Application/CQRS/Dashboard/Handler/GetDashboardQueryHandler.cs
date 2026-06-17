using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Dashboard.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Constants;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;

namespace IdentityHub.Application.CQRS.Dashboard.Handlers;

public sealed class GetDashboardQueryHandler
    : IRequestHandler<GetDashboardQuery, Result<DashboardResponse>>
{
    private readonly IDashboardRepository _repository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ISecurityAlertRepository _securityAlertRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IOptions<SmtpSettings> _smtpSettings;

    public GetDashboardQueryHandler(
        IDashboardRepository repository,
        IAuditLogRepository auditLogRepository,
        ISecurityAlertRepository securityAlertRepository,
        IRoleRepository roleRepository,
        IOptions<SmtpSettings> smtpSettings)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
        _securityAlertRepository = securityAlertRepository;
        _roleRepository = roleRepository;
        _smtpSettings = smtpSettings;
    }

    public async Task<Result<DashboardResponse>> Handle(
        GetDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var totalUsers = await _repository.GetTotalUsersAsync(cancellationToken);
        var activeUsers = await _repository.GetActiveUsersAsync(cancellationToken);
        var activeSessions = await _repository.GetActiveSessionsAsync(cancellationToken);
        var loginsToday = await _repository.GetLoginsTodayAsync(cancellationToken);
        var newUsers = await _repository.GetNewUsersThisWeekAsync(cancellationToken);
        var securityEvents = await _repository.GetSecurityEventsThisWeekAsync(cancellationToken);
        var openAlerts = await _repository.GetOpenSecurityAlertsAsync(cancellationToken);
        var auditedActionsToday = await _repository.GetAuditedActionsTodayAsync(cancellationToken);
        var pendingInvites = await _repository.GetPendingInvitesAsync(cancellationToken);
        var totalRoles = await _repository.GetTotalRolesAsync(cancellationToken);
        var suspiciousSessions = await _repository.GetSuspiciousSessionsAsync(cancellationToken);

        var lastWeekUsers = await _repository.GetNewUsersLastWeekAsync(cancellationToken);
        var lastWeekSessions = await _repository.GetSessionsLastWeekAsync(cancellationToken);
        var lastWeekSecurityEvents = await _repository.GetSecurityEventsLastWeekAsync(cancellationToken);
        var loginTrend = await _repository.GetLoginTrendAsync(30, cancellationToken);
        var newUsersTrend = await _repository.GetNewUsersTrendAsync(30, cancellationToken);

        var recentAuditLogs = await _auditLogRepository.GetPagedAsync(
            new AuditLogFilter(),
            1,
            5,
            cancellationToken);

        var recentSecurityAlerts = await _securityAlertRepository.GetPagedAsync(
            new SecurityAlertFilter(),
            1,
            5,
            cancellationToken);

        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        var roleClaims = new List<IList<System.Security.Claims.Claim>>();

        foreach (var role in roles)
        {
            var claims = await _roleRepository.GetClaimsAsync(role, cancellationToken);
            roleClaims.Add(claims);
        }

        var permissionCards = AppPermissions.All()
            .Select(permission => new DashboardPermissionCardResponse
            {
                Permission = permission,
                RolesGranted = roleClaims.Count(claims => claims.Any(claim =>
                    claim.Type == "permission" &&
                    string.Equals(claim.Value, permission, StringComparison.OrdinalIgnoreCase)))
            })
            .OrderByDescending(item => item.RolesGranted)
            .ThenBy(item => item.Permission)
            .ToList();

        var response = new DashboardResponse
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            ActiveSessions = activeSessions,
            LoginsToday = loginsToday,
            NewUsers = newUsers,
            SecurityEvents = securityEvents,
            OpenAlerts = openAlerts,
            AuditedActionsToday = auditedActionsToday,
            PendingInvites = pendingInvites,
            TotalRoles = totalRoles,
            SuspiciousSessions = suspiciousSessions,
            UsersGrowth = CalculateGrowth(lastWeekUsers, newUsers),
            SessionsGrowth = CalculateGrowth(lastWeekSessions, activeSessions),
            SecurityGrowth = CalculateGrowth(lastWeekSecurityEvents, securityEvents),
            SystemStatus = new DashboardSystemStatusResponse
            {
                ApiOnline = true,
                DatabaseConnected = true,
                EmailConfigured = HasEmailConfiguration()
            },
            PermissionCards = permissionCards,
            LoginTrend = loginTrend
                .Select(item => new DashboardTrendPointResponse
                {
                    Date = item.Date,
                    Value = item.Value
                })
                .ToList(),
            NewUsersTrend = newUsersTrend
                .Select(item => new DashboardTrendPointResponse
                {
                    Date = item.Date,
                    Value = item.Value
                })
                .ToList(),
            RecentAuditActions = recentAuditLogs.Items
                .Select(item => new AuditLogItemResponse
                {
                    Id = item.Id,
                    ActorUserId = item.ActorUserId,
                    Type = item.Type,
                    TargetId = item.TargetId,
                    Description = item.Description,
                    MetadataJson = item.MetadataJson,
                    CreatedAt = item.CreatedAt
                })
                .ToList(),
            RecentSecurityAlerts = recentSecurityAlerts.Items
                .Select(item => new SecurityAlertItemResponse
                {
                    Id = item.Id,
                    UserId = item.UserId,
                    Type = item.Type,
                    Severity = item.Severity,
                    Status = item.Status,
                    Description = item.Description,
                    CreatedAt = item.CreatedAt
                })
                .ToList()
        };

        return Result<DashboardResponse>.Success(response);
    }

    private bool HasEmailConfiguration()
    {
        var settings = _smtpSettings.Value;

        return !string.IsNullOrWhiteSpace(settings.Host)
            && settings.Port > 0
            && !string.IsNullOrWhiteSpace(settings.Username)
            && !string.IsNullOrWhiteSpace(settings.From);
    }

    private static double CalculateGrowth(int previous, int current)
    {
        if (previous == 0)
            return current > 0 ? 100 : 0;

        return Math.Round(((double)(current - previous) / previous) * 100, 2);
    }
}