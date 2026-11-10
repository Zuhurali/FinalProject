using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Services;

/// <summary>
/// Development-only email delivery that stores messages in an in-app inbox.
/// Used automatically when SMTP is not configured so registration can be tested
/// without User Secrets or provider credentials. Production never registers this type.
/// </summary>
public sealed class DevelopmentMailboxEmailSender : IEmailSender
{
    private readonly IDevelopmentMailStore _store;
    private readonly ILogger<DevelopmentMailboxEmailSender> _logger;

    public DevelopmentMailboxEmailSender(
        IDevelopmentMailStore store,
        ILogger<DevelopmentMailboxEmailSender> logger)
    {
        _store = store;
        _logger = logger;
    }

    public bool IsConfigured => true;

    public bool IsExternalConfigured => false;

    public Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new InvalidOperationException("A recipient email address is required.");
        }

        try
        {
            var message = _store.Add(toEmail, subject, htmlBody, textBody);
            _logger.LogInformation(
                "Development mailbox delivered message {MessageId} to {Recipient} ({Subject}).",
                message.Id,
                message.ToEmail,
                message.Subject);
            return Task.CompletedTask;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Development mailbox failed to store a message for {Recipient}.", toEmail);
            throw new InvalidOperationException(
                "The confirmation email could not be stored in the development mailbox.",
                exception);
        }
    }
}
