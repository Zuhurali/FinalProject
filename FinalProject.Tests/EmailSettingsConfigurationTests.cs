using FinalProject.MVC.Configuration;
using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinalProject.Tests;

public class EmailSettingsConfigurationTests
{
    [Fact]
    public void IsConfigured_requires_host_port_and_from_email()
    {
        Assert.False(new EmailSettings().IsConfigured);
        Assert.False(new EmailSettings { Host = "smtp.example.com", Port = 587 }.IsConfigured);
        Assert.False(new EmailSettings { Host = "smtp.example.com", Port = 0, FromEmail = "noreply@example.com" }.IsConfigured);
        Assert.True(new EmailSettings
        {
            Host = "smtp.example.com",
            Port = 587,
            FromEmail = "noreply@example.com"
        }.IsConfigured);
    }

    [Fact]
    public void IsConfigured_allows_username_and_password_to_be_optional_for_open_relays()
    {
        var settings = new EmailSettings
        {
            Host = "localhost",
            Port = 25,
            FromEmail = "noreply@example.com",
            FromName = "AutoVault",
            Encryption = "None"
        };

        Assert.True(settings.IsConfigured);
        Assert.False(settings.RequiresAuthentication);
        Assert.True(string.IsNullOrEmpty(settings.Username));
        Assert.True(string.IsNullOrEmpty(settings.Password));
        Assert.Equal("None", settings.Encryption);
    }

    [Fact]
    public void Resend_requires_username_and_password_before_it_is_configured()
    {
        var settings = new EmailSettings
        {
            Provider = "Resend",
            FromEmail = EmailSettings.ResendTestFromEmail
        };
        settings.ApplyKnownProviderDefaults();

        Assert.Equal(EmailSettings.ResendHost, settings.Host);
        Assert.Equal(EmailSettings.ResendUsername, settings.Username);
        Assert.True(settings.RequiresAuthentication);
        Assert.False(settings.IsConfigured);
        Assert.Contains(nameof(EmailSettings.Password), settings.DescribeMissingConfiguration());

        settings.Password = "re_test_key_not_real";
        Assert.True(settings.IsConfigured);
        Assert.True(settings.HasCredentials);
    }

    [Fact]
    public void Gmail_applies_smtp_defaults_and_requires_app_password()
    {
        var settings = new EmailSettings
        {
            Provider = "Gmail",
            FromEmail = "member@gmail.com"
        };
        settings.ApplyKnownProviderDefaults();

        Assert.Equal(EmailSettings.GmailHost, settings.Host);
        Assert.Equal(587, settings.Port);
        Assert.Equal("StartTls", settings.Encryption);
        Assert.Equal("member@gmail.com", settings.Username);
        Assert.True(settings.IsGmail);
        Assert.True(settings.RequiresAuthentication);
        Assert.False(settings.IsConfigured);

        settings.Password = "abcd efgh ijkl mnop";
        Assert.True(settings.IsConfigured);
    }

    [Fact]
    public void Default_encryption_is_start_tls_for_submission_ports()
    {
        Assert.Equal("StartTls", new EmailSettings().Encryption);
    }

    [Fact]
    public void ResolveSocketOptions_maps_resend_recommended_ports()
    {
        Assert.Equal(
            MailKit.Security.SecureSocketOptions.SslOnConnect,
            EmailSender.ResolveSocketOptions("SslOnConnect", 465));
        Assert.Equal(
            MailKit.Security.SecureSocketOptions.StartTls,
            EmailSender.ResolveSocketOptions("StartTls", 587));
        Assert.Equal(
            MailKit.Security.SecureSocketOptions.SslOnConnect,
            EmailSender.ResolveSocketOptions(null, 465));
    }

    [Fact]
    public async Task EmailSender_rejects_send_when_settings_are_empty()
    {
        IEmailSender sender = new EmailSender(
            Options.Create(new EmailSettings()),
            NullLogger<EmailSender>.Instance);

        Assert.False(sender.IsConfigured);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync("member@example.com", "Subject", "<p>Hi</p>", "Hi"));
        Assert.Contains("Email delivery is not configured", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProbeConnectivity_rejects_incomplete_resend_settings_without_network_io()
    {
        var sender = new EmailSender(
            Options.Create(new EmailSettings
            {
                Provider = "Resend",
                Host = EmailSettings.ResendHost,
                Port = 465,
                Username = EmailSettings.ResendUsername,
                FromEmail = EmailSettings.ResendTestFromEmail,
                Encryption = "SslOnConnect"
            }),
            NullLogger<EmailSender>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.ProbeConnectivityAsync());
        Assert.Contains(nameof(EmailSettings.Password), exception.Message, StringComparison.Ordinal);
    }
}
