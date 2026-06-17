namespace IdentityHub.Application.DTOs;

public sealed class DashboardResponse
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int ActiveSessions { get; set; }
    public int LoginsToday { get; set; }
    public int NewUsers { get; set; }
    public int SecurityEvents { get; set; }
    public int OpenAlerts { get; set; }
    public int AuditedActionsToday { get; set; }
    public int PendingInvites { get; set; }
    public int TotalRoles { get; set; }
    public int SuspiciousSessions { get; set; }

    public double UsersGrowth { get; set; }
    public double SessionsGrowth { get; set; }
    public double SecurityGrowth { get; set; }

    public DashboardSystemStatusResponse SystemStatus { get; set; } = new();
    public IReadOnlyList<DashboardPermissionCardResponse> PermissionCards { get; set; } = [];
    public IReadOnlyList<DashboardTrendPointResponse> LoginTrend { get; set; } = [];
    public IReadOnlyList<DashboardTrendPointResponse> NewUsersTrend { get; set; } = [];
    public IReadOnlyList<AuditLogItemResponse> RecentAuditActions { get; set; } = [];
    public IReadOnlyList<SecurityAlertItemResponse> RecentSecurityAlerts { get; set; } = [];
}

public sealed class DashboardSystemStatusResponse
{
    public bool ApiOnline { get; set; } = true;
    public bool DatabaseConnected { get; set; } = true;
    public bool EmailConfigured { get; set; }
}

public sealed class DashboardPermissionCardResponse
{
    public string Permission { get; set; } = string.Empty;
    public int RolesGranted { get; set; }
}

public sealed class DashboardTrendPointResponse
{
    public string Date { get; set; } = string.Empty;
    public int Value { get; set; }
}
