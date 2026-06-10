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

public sealed class ResendConfirmationCommandHandler : IRequestHandler<ResendConfirmationCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateBuilder _emailTemplateBuilder;
    private readonly IConfiguration _configuration;

    public ResendConfirmationCommandHandler(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IEmailTemplateBuilder emailTemplateBuilder,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _emailService = emailService;
        _emailTemplateBuilder = emailTemplateBuilder;
        _configuration = configuration;
    }

    public async Task<Result> Handle(
        ResendConfirmationCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
            return Result.Failure(Error.Create("Email.Required", "Email is required"));

        var email = command.Email.Trim().ToLowerInvariant();

        var user = await _userManager.FindByEmailAsync(email);

        if (user is null || user.IsDeleted || user.EmailConfirmed)
            return Result.Success();

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var encodedToken = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(token));

        var frontendBase = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:4200")
            .Trim()
            .TrimEnd('/');

        var confirmationLink =
            $"{frontendBase}/confirm-email?email={Uri.EscapeDataString(email)}&token={encodedToken}";

        var template = _emailTemplateBuilder.BuildConfirmEmailTemplate(
            actionUrl: confirmationLink,
            recipientName: user.FullName ?? email);

        await _emailService.SendAsync(
            email,
            template.Subject,
            template.BodyHtml);

        return Result.Success();
    }
}