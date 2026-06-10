using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace IdentityHub.Application.CQRS.Auth.Handlers;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthRepository _repo;
    private readonly ISecurityAlertService _securityAlertService;

    public ResetPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IAuthRepository repo,
        ISecurityAlertService securityAlertService)
    {
        _userManager = userManager;
        _repo = repo;
        _securityAlertService = securityAlertService;
    }

    public async Task<Result> Handle(ResetPasswordCommand cmd, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(cmd.Request.Email);

        if (user is null || user.IsDeleted)
            return Result.Failure(Error.Create("User.NotFound", "User not found"));

        string token;

        try
        {
            token = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(cmd.Request.Token));
        }
        catch (FormatException)
        {
            return Result.Failure(Error.Create("Password.InvalidTokenFormat", "Invalid token format"));
        }

        var result = await _userManager.ResetPasswordAsync(user, token, cmd.Request.NewPassword);

        if (!result.Succeeded)
            return Result.Failure(Error.Create("Password.ResetFailed", "Invalid token"));

        var sessions = await _repo.GetActiveSessionsAsync(user.Id, ct);
        foreach (var s in sessions) s.IsActive = false;

        var tokens = await _repo.GetActiveRefreshTokensAsync(user.Id, ct);
        foreach (var t in tokens) t.IsRevoked = true;

        await _repo.SaveChangesAsync(ct);

        await _securityAlertService.NotifyCriticalActionAsync(
            user,
            "Password reset",
            "Your account password was reset.",
            ct);

        return Result.Success();
    }
}