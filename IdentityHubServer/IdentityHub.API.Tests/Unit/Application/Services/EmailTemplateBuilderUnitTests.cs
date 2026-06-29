using System.Net;
using IdentityHub.Application.Services;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class EmailTemplateBuilderUnitTests
{
    private readonly EmailTemplateBuilder _builder = new();

    [Fact]
    public void BuildConfirmEmailTemplate_ShouldEncodeInputs_AndIncludeActionLink()
    {
        var url = "https://localhost/reset?email=a+b@identityhub.com&token=<token>";
        var recipient = "<admin>";

        var template = _builder.BuildConfirmEmailTemplate(url, recipient);

        Assert.Equal("Confirm your email", template.Subject);
        Assert.Contains("Confirm email", template.BodyHtml);
        Assert.Contains(WebUtility.HtmlEncode(url), template.BodyHtml);
        Assert.Contains("Hello &lt;admin&gt;", template.BodyHtml);
        Assert.DoesNotContain("Hello <admin>", template.BodyHtml);
    }

    [Fact]
    public void BuildSuspiciousLoginAlertTemplate_WithoutRecipient_ShouldUseFallbackAndEncodeDetails()
    {
        var details = "IP: <127.0.0.1>";

        var template = _builder.BuildSuspiciousLoginAlertTemplate(details);

        Assert.Equal("Security alert: suspicious login attempt", template.Subject);
        Assert.Contains("Hello there", template.BodyHtml);
        Assert.Contains("IP: &lt;127.0.0.1&gt;", template.BodyHtml);
        Assert.DoesNotContain("IP: <127.0.0.1>", template.BodyHtml);
    }
}
