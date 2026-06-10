using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Users.Commands;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace IdentityHub.Application.CQRS.Users.Handlers;

public sealed class InviteUserCommandHandler : IRequestHandler<InviteUserCommand, Result>
{
    private readonly IUserRepository _repository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateBuilder _emailTemplateBuilder;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IConfiguration _configuration;

    public InviteUserCommandHandler(
        IUserRepository repository,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IEmailTemplateBuilder emailTemplateBuilder,
        IAuditLogRepository auditLogRepository,
        IConfiguration configuration)
    {
        _repository = repository;
        _userManager = userManager;
        _emailService = emailService;
        _emailTemplateBuilder = emailTemplateBuilder;
        _auditLogRepository = auditLogRepository;
        _configuration = configuration;
    }

    public async Task<Result> Handle(InviteUserCommand command, CancellationToken cancellationToken)
    {
        var email = command.Request.Email.Trim().ToLowerInvariant();
        var fullName = command.Request.FullName?.Trim();

        var existingUser = await _repository.GetByEmailAsync(email, cancellationToken);

        ApplicationUser user;

        if (existingUser is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                IsActive = true,
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user);

            if (!createResult.Succeeded)
            {
                return Result.Failure(
                    Error.Create("User.CreateFailed", string.Join(", ", createResult.Errors.Select(e => e.Description))));
            }
        }
        else
        {
            if (existingUser.IsDeleted)
            {
                return Result.Failure(Error.Create("User.Deleted", "User is deleted and cannot be invited"));
            }

            if (await _userManager.HasPasswordAsync(existingUser))
            {
                return Result.Failure(Error.Create("User.AlreadyExists", "User already exists"));
            }

            user = existingUser;

            if (!string.IsNullOrWhiteSpace(fullName) && !string.Equals(user.FullName, fullName, StringComparison.Ordinal))
            {
                user.FullName = fullName;
                await _repository.UpdateAsync(user, cancellationToken);
            }
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));

        var frontendBase = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:4200")
            .Trim()
            .TrimEnd('/');

        var resetLink =
            $"{frontendBase}/reset-password?email={Uri.EscapeDataString(email)}&token={encodedToken}";

        var template = _emailTemplateBuilder.BuildInviteUserTemplate(
            actionUrl: resetLink,
            recipientName: user.FullName ?? email);

        await _emailService.SendAsync(
            email,
            template.Subject,
            template.BodyHtml,
            cancellationToken);

        await _auditLogRepository.WriteAsync(
            "Audit.User.Invited",
            $"User invited: id={user.Id}, email={email}",
            cancellationToken);

        return Result.Success();
    }
}
