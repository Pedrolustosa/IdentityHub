using IdentityHub.Domain.Constants;
using IdentityHub.Domain.Entities;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class DomainModelsAndConstantsUnitTests
{
    [Fact]
    public void AppPermissions_All_ShouldReturnCompleteUniqueCatalog()
    {
        var permissions = AppPermissions.All();

        Assert.Equal(27, permissions.Count);
        Assert.Equal(permissions.Count, permissions.Distinct(StringComparer.Ordinal).Count());

        Assert.Contains(AppPermissions.Users.View, permissions);
        Assert.Contains(AppPermissions.Roles.PermissionsUpdate, permissions);
        Assert.Contains(AppPermissions.Dashboard.View, permissions);
        Assert.Contains(AppPermissions.Sessions.Revoke, permissions);
        Assert.Contains(AppPermissions.SecurityEvents.Manage, permissions);
        Assert.Contains(AppPermissions.SecuritySettings.Update, permissions);
        Assert.Contains(AppPermissions.Permissions.CatalogView, permissions);
        Assert.Contains(AppPermissions.UserInvites.Resend, permissions);
    }

    [Fact]
    public void SecurityAlertEventTypes_All_ShouldMatchDeclaredConstants()
    {
        var all = SecurityAlertEventTypes.All();

        Assert.Equal(3, all.Count);
        Assert.Equal(SecurityAlertEventTypes.SuspiciousLogin, all[0]);
        Assert.Equal(SecurityAlertEventTypes.CriticalAction, all[1]);
        Assert.Equal(SecurityAlertEventTypes.RefreshTokenReuse, all[2]);
    }

    [Fact]
    public void SecurityEventSeverity_All_ShouldMatchDeclaredConstants()
    {
        var all = SecurityEventSeverity.All();

        Assert.Equal(new[]
        {
            SecurityEventSeverity.Low,
            SecurityEventSeverity.Medium,
            SecurityEventSeverity.High,
            SecurityEventSeverity.Critical
        }, all);
    }

    [Fact]
    public void SecurityEventStatus_All_ShouldMatchDeclaredConstants()
    {
        var all = SecurityEventStatus.All();

        Assert.Equal(new[]
        {
            SecurityEventStatus.Open,
            SecurityEventStatus.Reviewed,
            SecurityEventStatus.Ignored,
            SecurityEventStatus.Resolved
        }, all);
    }

    [Fact]
    public void Entity_Defaults_ShouldBeInitializedAsExpected()
    {
        var user = new ApplicationUser();
        var invite = new UserInvite();
        var securityEvent = new SecurityEvent();
        var securitySetting = new SecuritySetting();
        var refreshToken = new RefreshToken();
        var audit = new AuditLogEntry();
        var session = new UserSession();

        Assert.True(user.IsActive);
        Assert.Equal(1, user.PermissionVersion);

        Assert.Equal("Pending", invite.Status);
        Assert.True(invite.IsActive);

        Assert.Equal("Medium", securityEvent.Severity);
        Assert.Equal("Open", securityEvent.Status);

        Assert.Equal(30, securitySetting.AccessTokenMinutes);
        Assert.Equal(7, securitySetting.RefreshTokenDays);
        Assert.Equal(5, securitySetting.MaxLoginAttempts);
        Assert.Equal(15, securitySetting.LockDurationMinutes);
        Assert.True(securitySetting.RequireEmailConfirmation);

        Assert.Equal(string.Empty, refreshToken.TokenHash);
        Assert.Equal(string.Empty, refreshToken.UserId);
        Assert.Null(refreshToken.User);

        Assert.Equal(string.Empty, audit.ActorUserId);
        Assert.Equal(string.Empty, audit.Type);
        Assert.Equal(string.Empty, audit.Description);

        Assert.Equal(string.Empty, session.UserId);
        Assert.Equal(string.Empty, session.IpAddress);
        Assert.Equal(string.Empty, session.Browser);
        Assert.Equal(string.Empty, session.OperatingSystem);
    }
}
