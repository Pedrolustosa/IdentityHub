using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace IdentityHub.Application.CQRS.Auth.Handlers;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public ForgotPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var email = command.Request.Email.Trim().ToLowerInvariant();

        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
            return;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var link = BuildFrontendLink("reset-password", email, token);

        await _emailService.SendAsync(
            email,
            "Password Reset",
            $"Click here: <a href='{link}'>Reset Password</a>");
    }

    private string BuildFrontendLink(string path, string email, string token)
    {
        var frontendBase = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:4200")
            .Trim()
            .TrimEnd('/');

        return $"{frontendBase}/{path}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }
}