using FinalProject.MVC.Configuration;
using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinalProject.Tests;

public class CompositeEmailSenderTests
{
    [Fact]
    public async Task External_success_also_stores_a_development_mailbox_copy()
    {
        var store = new DevelopmentMailStore();
        var external = new RecordingExternalSender();
        var sender = CreateComposite(external, store, externalConfigured: true);

        await sender.SendAsync("user@gmail.com", "Subject", "<p>Hi</p>", "Hi");

        Assert.Single(external.Sent);
        Assert.Equal("user@gmail.com", Assert.Single(store.List()).ToEmail);
        Assert.True(sender.IsExternalConfigured);
    }

    [Fact]
    public async Task External_failure_is_not_swallowed_and_may_still_store_a_local_copy()
    {
        var store = new DevelopmentMailStore();
        var external = new RecordingExternalSender { Fail = true };
        var sender = CreateComposite(external, store, externalConfigured: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync("user@outlook.com", "Subject", "<p>Hi</p>", "Hi"));

        Assert.Single(store.List("user@outlook.com"));
    }

    [Fact]
    public async Task Without_external_settings_development_mailbox_is_used_only()
    {
        var store = new DevelopmentMailStore();
        var external = new RecordingExternalSender();
        var sender = CreateComposite(external, store, externalConfigured: false);

        await sender.SendAsync("user@yahoo.com", "Subject", "<p>Hi</p>", "Hi");

        Assert.Empty(external.Sent);
        Assert.Single(store.List("user@yahoo.com"));
        Assert.False(sender.IsExternalConfigured);
        Assert.True(sender.IsConfigured);
    }

    private static CompositeEmailSender CreateComposite(
        IEmailSender external,
        IDevelopmentMailStore store,
        bool externalConfigured)
    {
        var settings = externalConfigured
            ? new EmailSettings
            {
                Host = "smtp.resend.com",
                Port = 465,
                Username = "resend",
                Password = "re_test_key_not_real",
                FromEmail = "beth.t@example.com",
                Provider = "Resend",
                Encryption = "SslOnConnect"
            }
            : new EmailSettings();
        settings.ApplyKnownProviderDefaults();

        return new CompositeEmailSender(
            external,
            new FixedEnvironment(Environments.Development),
            Options.Create(settings),
            NullLogger<CompositeEmailSender>.Instance,
            new[] { store });
    }

    private sealed class RecordingExternalSender : IEmailSender
    {
        public bool Fail { get; set; }

        public List<(string To, string Subject, string Html, string Text)> Sent { get; } = new();

        public bool IsConfigured => true;

        public bool IsExternalConfigured => true;

        public Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            string textBody,
            CancellationToken cancellationToken = default)
        {
            if (Fail)
            {
                throw new InvalidOperationException("The email could not be delivered through the configured SMTP server.");
            }

            Sent.Add((toEmail, subject, htmlBody, textBody));
            return Task.CompletedTask;
        }
    }

    private sealed class FixedEnvironment : IWebHostEnvironment
    {
        public FixedEnvironment(string environmentName) => EnvironmentName = environmentName;

        public string ApplicationName { get; set; } = "FinalProject.MVC";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; }
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
