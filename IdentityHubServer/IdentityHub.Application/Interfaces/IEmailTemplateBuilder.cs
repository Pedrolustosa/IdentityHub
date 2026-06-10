using IdentityHub.Application.DTOs;

namespace IdentityHub.Application.Interfaces;

public interface IEmailTemplateBuilder
{
    EmailTemplate BuildConfirmEmailTemplate(string actionUrl, string? recipientName = null);
    EmailTemplate BuildResetPasswordTemplate(string actionUrl, string? recipientName = null);
    EmailTemplate BuildInviteUserTemplate(string actionUrl, string? recipientName = null);
}
