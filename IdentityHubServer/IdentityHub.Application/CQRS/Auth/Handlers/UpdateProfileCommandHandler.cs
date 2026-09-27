using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.Common.Validation;
using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IdentityHub.Application.CQRS.Auth.Handlers;

public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UpdateProfileCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result> Handle(UpdateProfileCommand cmd, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(cmd.UserId);

        if (user is null || user.IsDeleted)
            return Result.Failure(Error.Create("User.NotFound", "User not found"));

        user.FullName = cmd.Request.FullName?.Trim();
        user.PhoneNumber = UserContactValidation.NormalizePhoneNumber(cmd.Request.PhoneNumber);
        user.DateOfBirth = cmd.Request.DateOfBirth;

        await _userManager.UpdateAsync(user);

        return Result.Success();
    }
}
