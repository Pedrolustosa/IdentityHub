using IdentityHub.Domain.Interfaces;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Infrastructure.Services
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;

        public DashboardRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetTotalUsersAsync()
            => await _context.Users.CountAsync();

        public async Task<int> GetActiveSessionsAsync()
            => await _context.UserSessions
                .CountAsync(x => x.IsActive);

        public async Task<int> GetNewUsersThisWeekAsync()
        {
            var start = DateTime.UtcNow.AddDays(-7);

            return await _context.Users
                .CountAsync(x => x.CreatedAt >= start);
        }

        public async Task<int> GetNewUsersLastWeekAsync()
        {
            var start = DateTime.UtcNow.AddDays(-14);
            var end = DateTime.UtcNow.AddDays(-7);

            return await _context.Users
                .CountAsync(x => x.CreatedAt >= start && x.CreatedAt < end);
        }

        public async Task<int> GetSecurityEventsThisWeekAsync()
        {
            var start = DateTime.UtcNow.AddDays(-7);

            return await _context.SecurityEvents
                .CountAsync(x => x.CreatedAt >= start);
        }

        public async Task<int> GetSecurityEventsLastWeekAsync()
        {
            var start = DateTime.UtcNow.AddDays(-14);
            var end = DateTime.UtcNow.AddDays(-7);

            return await _context.SecurityEvents
                .CountAsync(x => x.CreatedAt >= start && x.CreatedAt < end);
        }
    }
}
