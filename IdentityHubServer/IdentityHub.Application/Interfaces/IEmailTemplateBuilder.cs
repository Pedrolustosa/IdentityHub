using IdentityHub.Application.DTOs;

namespace IdentityHub.Application.Interfaces;

public interface IEmailTemplateBuilder
{
    EmailTemplate BuildConfirmEmailTemplate(string actionUrl, string? recipientName = null);
    EmailTemplate BuildResetPasswordTemplate(string actionUrl, string? recipientName = null);
    EmailTemplate BuildInviteUserTemplate(string actionUrl, string? recipientName = null);
    EmailTemplate BuildSuspiciousLoginAlertTemplate(string details, string? recipientName = null);
    EmailTemplate BuildCriticalActionAlertTemplate(string actionTitle, string details, string? recipientName = null);
}
