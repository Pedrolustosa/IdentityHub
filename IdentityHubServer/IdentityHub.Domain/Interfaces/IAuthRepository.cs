using IdentityHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Domain.Interfaces
{
    public interface IAuthRepository
    {
        Task SaveRefreshTokenAsync(RefreshToken token);
        Task<RefreshToken?> GetRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(RefreshToken token);

        Task CreateSessionAsync(UserSession session);
        Task<List<UserSession>> GetActiveSessionsAsync(string userId);
        Task SaveChangesAsync();
        Task<List<RefreshToken>> GetActiveRefreshTokensAsync(string userId);
    }
}
