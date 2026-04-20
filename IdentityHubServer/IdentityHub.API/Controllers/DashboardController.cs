using IdentityHub.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Policy = "Users.View")]
        public async Task<IActionResult> Get()
        {
            var now = DateTime.UtcNow;

            var startCurrentWeek = now.AddDays(-7);
            var startLastWeek = now.AddDays(-14);

            var totalUsers = await _context.Users.CountAsync();

            var activeSessions = await _context.UserSessions
                .CountAsync(s => s.IsActive);

            var newUsersThisWeek = await _context.Users
                .CountAsync(u => u.CreatedAt >= startCurrentWeek);

            var newUsersLastWeek = await _context.Users
                .CountAsync(u => u.CreatedAt >= startLastWeek && u.CreatedAt < startCurrentWeek);

            var alertsThisWeek = await _context.SecurityEvents
                .CountAsync(e => e.CreatedAt >= startCurrentWeek);

            var alertsLastWeek = await _context.SecurityEvents
                .CountAsync(e => e.CreatedAt >= startLastWeek && e.CreatedAt < startCurrentWeek);

            double usersGrowth = CalculateGrowth(newUsersThisWeek, newUsersLastWeek);
            double sessionsGrowth = 0;
            double alertsGrowth = CalculateGrowth(alertsThisWeek, alertsLastWeek);

            return Ok(new
            {
                TotalUsers = totalUsers,
                ActiveSessions = activeSessions,
                NewUsersThisWeek = newUsersThisWeek,
                SecurityAlerts = alertsThisWeek,
                UsersGrowthPercent = usersGrowth,
                SessionsGrowthPercent = sessionsGrowth,
                AlertsGrowthPercent = alertsGrowth
            });
        }

        private double CalculateGrowth(int current, int previous)
        {
            if (previous == 0)
                return current > 0 ? 100 : 0;

            return Math.Round(((double)(current - previous) / previous) * 100, 2);
        }
    }
}