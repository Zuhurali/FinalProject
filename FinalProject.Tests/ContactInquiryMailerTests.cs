using FinalProject.MVC.Configuration;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinalProject.Tests;

public class ContactInquiryMailerTests
{
    [Fact]
    public async Task SendAsync_reports_not_configured_without_calling_smtp()
    {
        var sender = new FakeEmailSender { Configured = false };
        var mailer = Create(sender);

        var (sent, error) = await mailer.SendAsync(ValidModel());

        Assert.False(sent);
        Assert.Equal(ContactInquiryMailer.NotConfiguredMessage, error);
        Assert.Null(sender.LastTo);
        Assert.DoesNotContain("password", error, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("smtp", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendAsync_delivers_to_public_inbox_with_visitor_details()
    {
        var sender = new FakeEmailSender { Configured = true };
        var mailer = Create(sender);

        var (sent, error) = await mailer.SendAsync(ValidModel());

        Assert.True(sent);
        Assert.Null(error);
        Assert.Equal("barisking333@gmail.com", sender.LastTo);
        Assert.Contains("Ada Lovelace", sender.LastText, StringComparison.Ordinal);
        Assert.Contains("ada@example.com", sender.LastText, StringComparison.Ordinal);
        Assert.Contains("Brake pads", sender.LastText, StringComparison.Ordinal);
        Assert.Contains("Please call me about a filter.", sender.LastText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendAsync_hides_delivery_exceptions()
    {
        var sender = new FakeEmailSender
        {
            Configured = true,
            Throw = new InvalidOperationException("535 authentication failed for secret-user")
        };
        var mailer = Create(sender);

        var (sent, error) = await mailer.SendAsync(ValidModel());

        Assert.False(sent);
        Assert.Equal(ContactInquiryMailer.DeliveryFailedMessage, error);
        Assert.DoesNotContain("secret-user", error, StringComparison.Ordinal);
        Assert.DoesNotContain("535", error, StringComparison.Ordinal);
    }

    private static ContactInquiryMailer Create(FakeEmailSender sender) =>
        new(
            sender,
            new FixedSiteSettingsService(),
            NullLogger<ContactInquiryMailer>.Instance);

    private static ContactFormModel ValidModel() => new()
    {
        Name = "Ada Lovelace",
        Email = "ada@example.com",
        Phone = "+994 50 354 67 67",
        Car = "Brake pads",
        Message = "Please call me about a filter."
    };

    private sealed class FixedSiteSettingsService : ISiteSettingsService
    {
        public SitePublicSettings GetPublic() => new()
        {
            Contact = new DealershipContactSettings { Email = DealershipContactDefaults.Email }
        };

        public DealershipContactSettings GetContactSettings() => GetPublic().Contact;

        public Task EnsureSeedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<(bool Ok, string? Error)> SaveAsync(
            SiteSettingsEditViewModel model,
            IFormFile? logoFile,
            IFormFile? footerLogoFile,
            IFormFile? faviconFile,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(bool, string?)>((false, "not used"));
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public bool Configured { get; set; }

        public Exception? Throw { get; set; }

        public bool IsConfigured => Configured;

        public bool IsExternalConfigured => Configured;

        public string? LastTo { get; private set; }

        public string? LastText { get; private set; }

        public Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            string textBody,
            CancellationToken cancellationToken = default)
        {
            if (Throw != null)
            {
                throw Throw;
            }

            LastTo = toEmail;
            LastText = textBody;
            return Task.CompletedTask;
        }
    }
}
