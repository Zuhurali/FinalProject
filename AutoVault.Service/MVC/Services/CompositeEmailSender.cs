using FinalProject.MVC.Configuration;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FinalProject.MVC.Services;

/// <summary>
/// Sends through an external MailKit/SMTP sender when configured, and in Development also
/// stores a local mailbox copy. Never reports external success unless the external send completes.
/// </summary>
public sealed class CompositeEmailSender : IEmailSender
{
    private readonly IEmailSender _external;
    private readonly IDevelopmentMailStore? _mailbox;
    private readonly IWebHostEnvironment _environment;
    private readonly EmailSettings _settings;
    private readonly ILogger<CompositeEmailSender> _logger;

    public CompositeEmailSender(
        IEmailSender external,
        IWebHostEnvironment environment,
        IOptions<EmailSettings> settings,
        ILogger<CompositeEmailSender> logger,
        IEnumerable<IDevelopmentMailStore> mailboxes)
    {
        _external = external;
        _environment = environment;
        _settings = settings.Value;
        _logger = logger;
        _mailbox = mailboxes.FirstOrDefault();
    }

    public bool IsExternalConfigured => _settings.IsConfigured;

    public bool IsConfigured => IsExternalConfigured || CanUseDevelopmentMailbox;

    private bool CanUseDevelopmentMailbox =>
        _environment.IsDevelopment() && _mailbox != null;

    public async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default)
    {
        if (IsExternalConfigured)
        {
            try
            {
                await _external.SendAsync(toEmail, subject, htmlBody, textBody, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                TryStoreMailboxCopy(toEmail, subject, htmlBody, textBody, isExternalCopy: false);
                throw;
            }

            TryStoreMailboxCopy(toEmail, subject, htmlBody, textBody, isExternalCopy: true);
            return;
        }

        if (CanUseDevelopmentMailbox)
        {
            StoreMailboxCopy(toEmail, subject, htmlBody, textBody);
            _logger.LogInformation(
                "Stored confirmation mail for {Recipient} in the Development Mailbox only (no external SMTP configured).",
                toEmail);
            return;
        }

        _logger.LogWarning("Email delivery is not configured. The message to {Recipient} was not sent.", toEmail);
        throw new InvalidOperationException("Email delivery is not configured.");
    }

    private void TryStoreMailboxCopy(
        string toEmail,
        string subject,
        string htmlBody,
        string textBody,
        bool isExternalCopy)
    {
        if (!CanUseDevelopmentMailbox)
        {
            return;
        }

        try
        {
            StoreMailboxCopy(toEmail, subject, htmlBody, textBody);
            _logger.LogInformation(
                isExternalCopy
                    ? "Stored a Development Mailbox copy of the externally accepted message to {Recipient}."
                    : "Stored a Development Mailbox copy after external delivery failed for {Recipient}.",
                toEmail);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not store a Development Mailbox copy for {Recipient}.", toEmail);
        }
    }

    private void StoreMailboxCopy(string toEmail, string subject, string htmlBody, string textBody) =>
        _mailbox!.Add(toEmail, subject, htmlBody, textBody);
}
