using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IdentityHub.Application.CQRS.Auth.Handlers;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthRepository _repo;
    private readonly ISecurityAlertService _securityAlertService;
    private readonly IAuditLogRepository _auditLogRepository;

    public ChangePasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IAuthRepository repo,
        ISecurityAlertService securityAlertService,
        IAuditLogRepository auditLogRepository)
    {
        _userManager = userManager;
        _repo = repo;
        _securityAlertService = securityAlertService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result> Handle(ChangePasswordCommand cmd, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(cmd.UserId);

        if (user is null || user.IsDeleted)
            return Result.Failure(Error.Create("User.NotFound", "User not found"));

        var result = await _userManager.ChangePasswordAsync(
            user,
            cmd.Request.CurrentPassword,
            cmd.Request.NewPassword);

        if (!result.Succeeded)
            return Result.Failure(Error.Create("Password.ChangeFailed", "Invalid password"));

        var sessions = await _repo.GetActiveSessionsAsync(user.Id, ct);
        foreach (var s in sessions) s.IsActive = false;

        await _repo.SaveChangesAsync(ct);

        await _auditLogRepository.WriteAsync(
            "Audit.User.PasswordChanged",
            $"User password changed: id={user.Id}, email={user.Email}. All active sessions were ended.",
            user.Id,
            new { userId = user.Id, email = user.Email, endedSessions = sessions.Count },
            ct);

        await _securityAlertService.NotifyCriticalActionAsync(
            user,
            "Password changed",
            "Your account password was changed successfully.",
            ct);

        return Result.Success();
    }
}