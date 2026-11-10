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

    public bool IsExternalConfigured => _settings.IsConfigured;

    public async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
        {
            var missing = string.Join(", ", _settings.DescribeMissingConfiguration());
            _logger.LogWarning(
                "Email delivery is not configured (missing: {Missing}). The message to {Recipient} was not sent.",
                missing,
                toEmail);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(missing)
                    ? "Email delivery is not configured."
                    : $"Email delivery is not configured. Missing: {missing}.");
        }

        var host = _settings.Host.Trim();
        var fromEmail = _settings.FromEmail.Trim();
        var fromName = string.IsNullOrWhiteSpace(_settings.FromName) ? "AutoVault" : _settings.FromName.Trim();
        var username = _settings.Username?.Trim() ?? string.Empty;
        var encryption = ResolveSocketOptions(_settings.Encryption, _settings.Port);

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
            _logger.LogInformation(
                "Connecting to SMTP {Host}:{Port} ({Encryption}) as {Username} to send to {Recipient}.",
                host,
                _settings.Port,
                encryption,
                string.IsNullOrWhiteSpace(username) ? "(anonymous)" : username,
                toEmail);

            await client.ConnectAsync(host, _settings.Port, encryption, cancellationToken);
            if (!string.IsNullOrWhiteSpace(username))
            {
                await client.AuthenticateAsync(username, _settings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            _logger.LogInformation("SMTP provider accepted the message for {Recipient}.", toEmail);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var detail = DescribeSmtpFailure(exception);
            _logger.LogError(exception, "SMTP delivery failed for {Recipient}: {Detail}", toEmail, detail);
            throw new InvalidOperationException(detail, exception);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Opens an SMTP session and authenticates without sending mail.
    /// Used by safe configuration checks; does not log secrets.
    /// </summary>
    public async Task ProbeConnectivityAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
        {
            throw new InvalidOperationException(
                $"Email delivery is not configured. Missing: {string.Join(", ", _settings.DescribeMissingConfiguration())}.");
        }

        var host = _settings.Host.Trim();
        var username = _settings.Username?.Trim() ?? string.Empty;
        var encryption = ResolveSocketOptions(_settings.Encryption, _settings.Port);

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(host, _settings.Port, encryption, cancellationToken);
            if (!string.IsNullOrWhiteSpace(username))
            {
                await client.AuthenticateAsync(username, _settings.Password, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidOperationException(DescribeSmtpFailure(exception), exception);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
        }
    }

    public static SecureSocketOptions ResolveSocketOptions(string? encryption, int port)
    {
        if (string.IsNullOrWhiteSpace(encryption))
        {
            return port is 465 or 2465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;
        }

        return encryption.Trim().ToLowerInvariant() switch
        {
            "none" => SecureSocketOptions.None,
            "auto" => SecureSocketOptions.Auto,
            "sslonconnect" or "ssl" => SecureSocketOptions.SslOnConnect,
            _ => SecureSocketOptions.StartTls
        };
    }

    internal static string DescribeSmtpFailure(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is AuthenticationException)
            {
                return "SMTP authentication failed. Check EmailSettings:Username and EmailSettings:Password (for Resend, Username must be 'resend' and Password must be your API key).";
            }

            if (current is SmtpCommandException smtp)
            {
                return $"SMTP provider rejected the message ({(int)smtp.StatusCode} {smtp.StatusCode}): {smtp.Message}";
            }

            if (current is SmtpProtocolException protocol)
            {
                return $"SMTP protocol error: {protocol.Message}";
            }

            if (current is SslHandshakeException ssl)
            {
                return $"SMTP TLS handshake failed: {ssl.Message}. For Resend use Port 465 with Encryption SslOnConnect, or Port 587 with StartTls.";
            }
        }

        return $"The email could not be delivered through the configured SMTP server: {exception.Message}";
    }
}
