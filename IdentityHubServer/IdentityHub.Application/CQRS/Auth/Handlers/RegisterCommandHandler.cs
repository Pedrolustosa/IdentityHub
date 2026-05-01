using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace IdentityHub.Application.CQRS.Auth.Handlers;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    public RegisterCommandHandler(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IConfiguration config)
    {
        _userManager = userManager;
        _emailService = emailService;
        _config = config;
    }

    public async Task<Result> Handle(RegisterCommand command, CancellationToken ct)
    {
        var email = command.Request.Email.Trim().ToLowerInvariant();

        var exists = await _userManager.FindByEmailAsync(email);
        if (exists != null)
            return Result.Failure(Error.Create("User.Exists", "User already exists"));

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            FullName = command.Request.FullName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, command.Request.Password);

        if (!result.Succeeded)
            return Result.Failure(Error.Create("User.CreateFailed",
                string.Join(", ", result.Errors.Select(e => e.Description))));

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var baseUrl = (_config["Frontend:BaseUrl"] ?? "").TrimEnd('/');
        var link = $"{baseUrl}/confirm-email?email={email}&token={encoded}";

        await _emailService.SendAsync(email, "Confirm Email", $"<a href='{link}'>Confirm</a>");

        return Result.Success();
    }
}