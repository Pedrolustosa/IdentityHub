using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.DTOs
{
    public class DashboardResponse
    {
        public int TotalUsers { get; set; }
        public int ActiveSessions { get; set; }
        public int NewUsersThisWeek { get; set; }
        public int SecurityAlerts { get; set; }

        public double UsersGrowthPercent { get; set; }
        public double SessionsGrowthPercent { get; set; }
        public double AlertsGrowthPercent { get; set; }
    }
}
