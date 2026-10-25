using System.Net;
using FinalProject.MVC.Configuration;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FinalProject.MVC.Services;

public sealed class ContactInquiryMailer : IContactInquiryMailer
{
    public const string NotConfiguredMessage =
        "We couldn't send your message because email delivery is not set up. Please email zuhraliim@code.edu.az directly.";

    public const string DeliveryFailedMessage =
        "We couldn't send your message right now. Please try again, or email zuhraliim@code.edu.az directly.";

    private readonly IEmailSender _emailSender;
    private readonly DealershipContactSettings _contact;
    private readonly ILogger<ContactInquiryMailer> _logger;

    public ContactInquiryMailer(
        IEmailSender emailSender,
        IOptions<DealershipContactSettings> contact,
        ILogger<ContactInquiryMailer> logger)
    {
        _emailSender = emailSender;
        _contact = contact.Value;
        _logger = logger;
    }

    public async Task<(bool Sent, string? Error)> SendAsync(
        ContactFormModel model,
        CancellationToken cancellationToken = default)
    {
        var inbox = string.IsNullOrWhiteSpace(_contact.Email)
            ? DealershipContactDefaults.Email
            : _contact.Email.Trim();

        if (!_emailSender.IsConfigured)
        {
            _logger.LogWarning("Contact form email was not sent because SMTP is not configured.");
            return (false, NotConfiguredMessage);
        }

        var subject = $"AutoVault contact: {model.Name}";
        var text = $"""
            Name: {model.Name}
            Email: {model.Email}
            Phone: {model.Phone}
            Interest: {model.Car}

            {model.Message}
            """;
        var html = $"""
            <p><strong>Name:</strong> {Encode(model.Name)}</p>
            <p><strong>Email:</strong> {Encode(model.Email)}</p>
            <p><strong>Phone:</strong> {Encode(model.Phone)}</p>
            <p><strong>Interest:</strong> {Encode(model.Car)}</p>
            <p>{Encode(model.Message).Replace("\n", "<br>", StringComparison.Ordinal)}</p>
            """;

        try
        {
            await _emailSender.SendAsync(inbox, subject, html, text, cancellationToken);
            return (true, null);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Contact form email to {Inbox} failed.", inbox);
            return (false, DeliveryFailedMessage);
        }
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
