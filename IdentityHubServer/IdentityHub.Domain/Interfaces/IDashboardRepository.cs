using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Domain.Interfaces
{
    public interface IDashboardRepository
    {
        Task<int> GetTotalUsersAsync();
        Task<int> GetActiveSessionsAsync();
        Task<int> GetNewUsersThisWeekAsync();
        Task<int> GetNewUsersLastWeekAsync();

        Task<int> GetSecurityEventsThisWeekAsync();
        Task<int> GetSecurityEventsLastWeekAsync();
    }
}
