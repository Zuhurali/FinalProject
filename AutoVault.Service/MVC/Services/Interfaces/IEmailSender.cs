namespace FinalProject.MVC.Services.Interfaces;

public interface IEmailSender
{
    /// <summary>
    /// True when this sender can accept a message (external SMTP and/or Development Mailbox).
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// True only when an external SMTP/API provider is configured.
    /// Local mailbox-only mode must leave this false.
    /// </summary>
    bool IsExternalConfigured { get; }

    Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default);
}
