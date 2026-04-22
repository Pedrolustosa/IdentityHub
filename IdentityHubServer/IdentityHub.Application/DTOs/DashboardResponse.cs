using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.DTOs
{
    public class DashboardResponse
    {
        public int TotalUsers { get; set; }
        public int ActiveSessions { get; set; }
        public int NewUsers { get; set; }
        public double NewUsersGrowth { get; set; }

        public int SecurityAlerts { get; set; }
        public double SecurityGrowth { get; set; }
    }
}
