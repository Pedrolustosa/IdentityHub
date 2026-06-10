using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using System.Net;
using System.Text;

namespace IdentityHub.Application.Services;

public sealed class EmailTemplateBuilder : IEmailTemplateBuilder
{
    public EmailTemplate BuildConfirmEmailTemplate(string actionUrl, string? recipientName = null)
        => BuildTemplate(
            subject: "Confirm your email",
            preheader: "Confirm your email to activate your IdentityHub account.",
            title: "Confirm your email",
            message: "Click the button below to confirm your email and continue using IdentityHub.",
            actionLabel: "Confirm email",
            actionUrl: actionUrl,
            recipientName: recipientName);

    public EmailTemplate BuildResetPasswordTemplate(string actionUrl, string? recipientName = null)
        => BuildTemplate(
            subject: "Reset your password",
            preheader: "Use this secure link to set a new password.",
            title: "Reset your password",
            message: "Click the button below to choose a new password for your IdentityHub account.",
            actionLabel: "Reset password",
            actionUrl: actionUrl,
            recipientName: recipientName);

    public EmailTemplate BuildInviteUserTemplate(string actionUrl, string? recipientName = null)
        => BuildTemplate(
            subject: "You are invited to IdentityHub",
            preheader: "You have been invited to access IdentityHub.",
            title: "You were invited",
            message: "An administrator invited you to IdentityHub. Click the button below to set your password and activate your access.",
            actionLabel: "Set password",
            actionUrl: actionUrl,
            recipientName: recipientName);

    private static EmailTemplate BuildTemplate(
        string subject,
        string preheader,
        string title,
        string message,
        string actionLabel,
        string actionUrl,
        string? recipientName)
    {
        var safeActionUrl = WebUtility.HtmlEncode(actionUrl);
        var safeRecipientName = string.IsNullOrWhiteSpace(recipientName)
            ? "there"
            : WebUtility.HtmlEncode(recipientName.Trim());

        var body = new StringBuilder();
        body.Append("<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'>");
        body.Append("<title>").Append(WebUtility.HtmlEncode(subject)).Append("</title></head>");
        body.Append("<body style='margin:0;background:#f3f6fb;font-family:Segoe UI,Arial,sans-serif;color:#0f172a;'>");
        body.Append("<div style='display:none;max-height:0;overflow:hidden;opacity:0;'>").Append(WebUtility.HtmlEncode(preheader)).Append("</div>");
        body.Append("<table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='padding:24px 12px;'><tr><td align='center'>");
        body.Append("<table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='max-width:600px;background:#ffffff;border:1px solid #e2e8f0;border-radius:12px;overflow:hidden;'>");
        body.Append("<tr><td style='background:#0f172a;padding:18px 24px;color:#ffffff;font-weight:600;letter-spacing:.3px;'>IdentityHub</td></tr>");
        body.Append("<tr><td style='padding:24px;'>");
        body.Append("<p style='margin:0 0 8px 0;color:#475569;'>Hello ").Append(safeRecipientName).Append(",</p>");
        body.Append("<h1 style='margin:0 0 12px 0;font-size:22px;line-height:1.3;color:#0f172a;'>").Append(WebUtility.HtmlEncode(title)).Append("</h1>");
        body.Append("<p style='margin:0 0 20px 0;color:#475569;line-height:1.6;'>").Append(WebUtility.HtmlEncode(message)).Append("</p>");
        body.Append("<p style='margin:0 0 20px 0;'><a href='").Append(safeActionUrl).Append("' style='display:inline-block;background:#2563eb;color:#ffffff;text-decoration:none;padding:12px 16px;border-radius:8px;font-weight:600;'>").Append(WebUtility.HtmlEncode(actionLabel)).Append("</a></p>");
        body.Append("<p style='margin:0;color:#64748b;font-size:12px;line-height:1.5;'>If the button does not work, copy and paste this link into your browser:<br><a href='").Append(safeActionUrl).Append("' style='color:#2563eb;word-break:break-all;'>").Append(safeActionUrl).Append("</a></p>");
        body.Append("</td></tr>");
        body.Append("<tr><td style='padding:16px 24px;background:#f8fafc;color:#64748b;font-size:12px;'>This is an automated message from IdentityHub.</td></tr>");
        body.Append("</table></td></tr></table></body></html>");

        return new EmailTemplate(subject, body.ToString());
    }
}
