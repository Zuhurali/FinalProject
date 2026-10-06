namespace FinalProject.MVC.Services.Interfaces;

public interface IEmailSender
{
    bool IsConfigured { get; }

    Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default);
}
