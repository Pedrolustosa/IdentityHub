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

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateBuilder _emailTemplateBuilder;
    private readonly IConfiguration _config;

    public ForgotPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IEmailTemplateBuilder emailTemplateBuilder,
        IConfiguration config)
    {
        _userManager = userManager;
        _emailService = emailService;
        _emailTemplateBuilder = emailTemplateBuilder;
        _config = config;
    }

    public async Task<Result> Handle(ForgotPasswordCommand cmd, CancellationToken ct)
    {
        var email = cmd.Request.Email.Trim().ToLowerInvariant();

        var user = await _userManager.FindByEmailAsync(email);

        if (user is null || user.IsDeleted)
            return Result.Success();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var baseUrl = (_config["Frontend:BaseUrl"] ?? "").TrimEnd('/');
        var link = $"{baseUrl}/reset-password?email={email}&token={encoded}";

        var template = _emailTemplateBuilder.BuildResetPasswordTemplate(
            actionUrl: link,
            recipientName: user.FullName ?? email);

        await _emailService.SendAsync(email, template.Subject, template.BodyHtml);

        return Result.Success();
    }
}