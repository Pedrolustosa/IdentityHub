using IdentityHub.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardResponse> GetAsync();
    }
}
