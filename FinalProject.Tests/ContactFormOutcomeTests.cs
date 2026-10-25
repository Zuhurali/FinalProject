using FinalProject.MVC.Commerce;
using FinalProject.MVC.Services;
using Xunit;

namespace FinalProject.Tests;

public class ContactFormOutcomeTests
{
    [Fact]
    public void AfterSuccessfulSave_reports_sent_only_when_email_went_out()
    {
        var outcome = ContactFormOutcome.AfterSuccessfulSave(emailed: true, emailUnavailable: false);

        Assert.True(outcome.Success);
        Assert.True(outcome.EmailSent);
        Assert.Equal(200, outcome.StatusCode);
        Assert.Equal(ContactFormOutcome.SentSuccess, outcome.Message);
        Assert.Contains("was sent", outcome.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AfterSuccessfulSave_does_not_claim_email_when_smtp_is_unavailable()
    {
        var outcome = ContactFormOutcome.AfterSuccessfulSave(emailed: false, emailUnavailable: true);

        Assert.True(outcome.Success);
        Assert.False(outcome.EmailSent);
        Assert.Equal(ContactFormOutcome.SavedEmailUnavailable, outcome.Message);
        Assert.DoesNotContain("was sent", outcome.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("saved", outcome.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unavailable", outcome.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(ContactInquiryMailer.NotConfiguredMessage, outcome.Message);
    }

    [Fact]
    public void AfterSuccessfulSave_does_not_claim_email_when_delivery_fails()
    {
        var outcome = ContactFormOutcome.AfterSuccessfulSave(emailed: false, emailUnavailable: false);

        Assert.True(outcome.Success);
        Assert.False(outcome.EmailSent);
        Assert.Equal(ContactFormOutcome.SavedEmailFailed, outcome.Message);
        Assert.DoesNotContain("was sent", outcome.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("saved", outcome.Message, StringComparison.OrdinalIgnoreCase);
    }
}
