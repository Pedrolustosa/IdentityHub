using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Constants;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;

namespace IdentityHub.Application.Services;

public sealed class SecurityAlertService : ISecurityAlertService
{
    private readonly IAuthRepository _authRepository;
    private readonly IClientDeviceInfoProvider _clientDeviceInfoProvider;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateBuilder _emailTemplateBuilder;

    public SecurityAlertService(
        IAuthRepository authRepository,
        IClientDeviceInfoProvider clientDeviceInfoProvider,
        IEmailService emailService,
        IEmailTemplateBuilder emailTemplateBuilder)
    {
        _authRepository = authRepository;
        _clientDeviceInfoProvider = clientDeviceInfoProvider;
        _emailService = emailService;
        _emailTemplateBuilder = emailTemplateBuilder;
    }

    public async Task NotifySuspiciousLoginAsync(
        ApplicationUser user,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var (ipAddress, browser, operatingSystem) = _clientDeviceInfoProvider.GetCurrent();
        var description =
            $"Suspicious login attempt detected. reason={reason}, ip={ipAddress}, browser={browser}, os={operatingSystem}, at={now:O}";

        await _authRepository.AddSecurityEventAsync(new SecurityEvent
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = SecurityAlertEventTypes.SuspiciousLogin,
            Description = description,
            CreatedAt = now
        }, cancellationToken);

        await _authRepository.SaveChangesAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(user.Email))
            return;

        var template = _emailTemplateBuilder.BuildSuspiciousLoginAlertTemplate(
            details: $"Time (UTC): {now:yyyy-MM-dd HH:mm:ss} | IP: {ipAddress} | Browser: {browser} | OS: {operatingSystem}.",
            recipientName: user.FullName ?? user.Email);

        await TrySendEmailAsync(user.Email, template.Subject, template.BodyHtml, cancellationToken);
    }

    public async Task NotifyCriticalActionAsync(
        ApplicationUser user,
        string actionTitle,
        string details,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var (ipAddress, browser, operatingSystem) = _clientDeviceInfoProvider.GetCurrent();
        var description =
            $"Critical action detected. action={actionTitle}, details={details}, ip={ipAddress}, browser={browser}, os={operatingSystem}, at={now:O}";

        await _authRepository.AddSecurityEventAsync(new SecurityEvent
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = SecurityAlertEventTypes.CriticalAction,
            Description = description,
            CreatedAt = now
        }, cancellationToken);

        await _authRepository.SaveChangesAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(user.Email))
            return;

        var template = _emailTemplateBuilder.BuildCriticalActionAlertTemplate(
            actionTitle,
            details: $"{details} | Time (UTC): {now:yyyy-MM-dd HH:mm:ss} | IP: {ipAddress} | Browser: {browser} | OS: {operatingSystem}.",
            recipientName: user.FullName ?? user.Email);

        await TrySendEmailAsync(user.Email, template.Subject, template.BodyHtml, cancellationToken);
    }

    private async Task TrySendEmailAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        try
        {
            await _emailService.SendAsync(to, subject, body, cancellationToken);
        }
        catch
        {
            // Security events are persisted even when outbound SMTP is unavailable.
        }
    }
}