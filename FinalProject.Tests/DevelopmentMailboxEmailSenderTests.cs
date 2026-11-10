using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinalProject.Tests;

public class DevelopmentMailboxEmailSenderTests
{
    [Fact]
    public async Task SendAsync_stores_a_message_addressed_to_the_recipient()
    {
        var store = new DevelopmentMailStore();
        IEmailSender sender = new DevelopmentMailboxEmailSender(
            store,
            NullLogger<DevelopmentMailboxEmailSender>.Instance);

        Assert.True(sender.IsConfigured);
        await sender.SendAsync(
            "member@example.com",
            AccountEmailContent.ConfirmationSubject,
            "<a href=\"https://autovault.example/Account/ConfirmEmail?userId=1&token=abc\">Confirm Email</a>",
            "Confirm Email: https://autovault.example/Account/ConfirmEmail?userId=1&token=abc");

        var message = Assert.Single(store.List("member@example.com"));
        Assert.Equal("member@example.com", message.ToEmail);
        Assert.Equal(AccountEmailContent.ConfirmationSubject, message.Subject);
        Assert.Contains("Confirm Email", message.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("/Account/ConfirmEmail?", message.TextBody, StringComparison.Ordinal);
        Assert.Same(message, store.Find(message.Id));
        Assert.Empty(store.List("other@example.com"));
    }

    [Fact]
    public async Task SendAsync_rejects_a_missing_recipient()
    {
        IEmailSender sender = new DevelopmentMailboxEmailSender(
            new DevelopmentMailStore(),
            NullLogger<DevelopmentMailboxEmailSender>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync(" ", "Subject", "<p>Hi</p>", "Hi"));
    }
}
