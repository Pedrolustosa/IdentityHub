using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace IdentityHub.Application.CQRS.Auth.Handlers;

public sealed class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ConfirmEmailCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result> Handle(ConfirmEmailCommand cmd, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(cmd.Email);

        if (user == null)
            return Result.Failure(Error.Create("User.NotFound", "User not found"));

        string token;

        try
        {
            token = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(cmd.Token));
        }
        catch (FormatException)
        {
            return Result.Failure(Error.Create("Email.InvalidTokenFormat", "Invalid token format"));
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);

        if (!result.Succeeded)
            return Result.Failure(Error.Create("Email.Invalid", "Invalid token"));

        return Result.Success();
    }
}