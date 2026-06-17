using IdentityHub.Domain.Entities;

namespace IdentityHub.Application.Interfaces;

public interface ISecurityAlertService
{
    Task NotifySuspiciousLoginAsync(ApplicationUser user, string reason, CancellationToken cancellationToken = default);
    Task NotifyCriticalActionAsync(ApplicationUser user, string actionTitle, string details, CancellationToken cancellationToken = default);
    Task NotifyRefreshTokenReuseAsync(ApplicationUser user, Guid sessionId, CancellationToken cancellationToken = default);
}