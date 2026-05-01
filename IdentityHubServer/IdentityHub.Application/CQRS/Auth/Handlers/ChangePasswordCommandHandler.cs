using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IdentityHub.Application.CQRS.Auth.Handlers;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthRepository _repo;

    public ChangePasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IAuthRepository repo)
    {
        _userManager = userManager;
        _repo = repo;
    }

    public async Task<Result> Handle(ChangePasswordCommand cmd, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(cmd.UserId);

        var result = await _userManager.ChangePasswordAsync(
            user,
            cmd.Request.CurrentPassword,
            cmd.Request.NewPassword);

        if (!result.Succeeded)
            return Result.Failure(Error.Create("Password.ChangeFailed", "Invalid password"));

        var sessions = await _repo.GetActiveSessionsAsync(user.Id, ct);
        foreach (var s in sessions) s.IsActive = false;

        await _repo.SaveChangesAsync(ct);

        return Result.Success();
    }
}