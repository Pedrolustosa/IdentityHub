namespace IdentityHub.Application.DTOs;

public sealed class DashboardResponse
{
    public int TotalUsers { get; set; }
    public int ActiveSessions { get; set; }
    public int NewUsers { get; set; }
    public int SecurityEvents { get; set; }

    public double UsersGrowth { get; set; }
    public double SessionsGrowth { get; set; }
    public double SecurityGrowth { get; set; }
}
