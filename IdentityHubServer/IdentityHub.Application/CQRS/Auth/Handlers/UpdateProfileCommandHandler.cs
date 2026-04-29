using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace IdentityHub.Application.CQRS.Auth.Handlers;

public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public UpdateProfileCommandHandler(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(command.UserId);

        if (user is null)
            throw new UnauthorizedAccessException();

        var request = command.Request;

        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName.Trim();

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim().ToLowerInvariant();

            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                var existingUser = await _userManager.FindByEmailAsync(email);

                if (existingUser is not null)
                    throw new InvalidOperationException("Email already in use");

                user.Email = email;
                user.UserName = email;
                user.NormalizedEmail = email.ToUpperInvariant();
                user.NormalizedUserName = email.ToUpperInvariant();
                user.EmailConfirmed = false;

                await SendConfirmationEmail(user, cancellationToken);
            }
        }

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    private async Task SendConfirmationEmail(ApplicationUser user, CancellationToken cancellationToken)
    {
        var email = user.Email!;

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var frontendBase = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:4200")
            .Trim()
            .TrimEnd('/');

        var link =
            $"{frontendBase}/confirm-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

        await _emailService.SendAsync(
            email,
            "Confirm your account",
            $"Click here: <a href='{link}'>Confirm Email</a>");
    }
}