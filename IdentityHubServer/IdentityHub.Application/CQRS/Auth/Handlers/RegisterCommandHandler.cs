using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace IdentityHub.Application.CQRS.Auth.Handlers;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public RegisterCommandHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var email = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
            throw new InvalidOperationException("User already exists");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = request.FullName?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(e => e.Description)));

        if (await _roleManager.RoleExistsAsync("User"))
            await _userManager.AddToRoleAsync(user, "User");

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var link = BuildFrontendLink("confirm-email", email, token);

        await _emailService.SendAsync(
            email,
            "Confirm your account",
            $"Click here: <a href='{link}'>Confirm Email</a>");
    }

    private string BuildFrontendLink(string path, string email, string token)
    {
        var frontendBase = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:4200")
            .Trim()
            .TrimEnd('/');

        return $"{frontendBase}/{path}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }
}