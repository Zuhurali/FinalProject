using FinalProject.MVC.Configuration;
using FinalProject.MVC.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FinalProject.MVC.Services;

public sealed class EmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<EmailSettings> settings, ILogger<EmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public bool IsConfigured => _settings.IsConfigured;

    public async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
        {
            _logger.LogWarning("Email delivery is not configured. The message to {Recipient} was not sent.", toEmail);
            throw new InvalidOperationException("Email delivery is not configured.");
        }

        var host = _settings.Host.Trim();
        var fromEmail = _settings.FromEmail.Trim();
        var fromName = string.IsNullOrWhiteSpace(_settings.FromName) ? "AutoVault" : _settings.FromName.Trim();
        var username = _settings.Username?.Trim() ?? string.Empty;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = textBody
        }.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(host, _settings.Port, SecureSocketOptions.StartTls, cancellationToken);
            if (!string.IsNullOrWhiteSpace(username))
            {
                await client.AuthenticateAsync(username, _settings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "SMTP delivery failed for {Recipient}.", toEmail);
            throw new InvalidOperationException("The email could not be delivered through the configured SMTP server.", exception);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
        }
    }
}
