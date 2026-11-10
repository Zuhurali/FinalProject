namespace FinalProject.MVC.Configuration;

/// <summary>
/// SMTP settings for Gmail, Resend, or Brevo (MailKit).
/// Provide Host, Port, FromEmail, Username, and Password through User Secrets
/// or environment variables (EmailSettings__Host, EmailSettings__Password, …).
/// Do not commit real credentials.
/// </summary>
public sealed class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public const string GmailHost = "smtp.gmail.com";
    public const string ResendHost = "smtp.resend.com";
    public const string ResendUsername = "resend";
    public const string ResendTestFromEmail = "beth.t@example.com";

    /// <summary>Optional label for logs/docs (Gmail, Resend, Brevo, …).</summary>
    public string Provider { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "AutoVault";

    /// <summary>
    /// SMTP transport security: StartTls (port 587), SslOnConnect (port 465), or None (local relays).
    /// </summary>
    public string Encryption { get; set; } = "StartTls";

    public bool IsGmail =>
        Provider.Equals("Gmail", StringComparison.OrdinalIgnoreCase)
        || Host.Contains("gmail.com", StringComparison.OrdinalIgnoreCase);

    public bool IsResend =>
        Provider.Equals("Resend", StringComparison.OrdinalIgnoreCase)
        || Host.Contains("resend.com", StringComparison.OrdinalIgnoreCase);

    public bool IsBrevo =>
        Provider.Equals("Brevo", StringComparison.OrdinalIgnoreCase)
        || Host.Contains("brevo.com", StringComparison.OrdinalIgnoreCase)
        || Host.Contains("sendinblue.com", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Authenticated providers (Gmail/Resend/Brevo) require Username + Password.
    /// Open local relays may omit credentials.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host)
        && Port is > 0 and <= 65535
        && !string.IsNullOrWhiteSpace(FromEmail)
        && (!RequiresAuthentication || HasCredentials);

    public bool RequiresAuthentication =>
        IsGmail
        || IsResend
        || IsBrevo
        || !string.IsNullOrWhiteSpace(Username)
        || !string.IsNullOrWhiteSpace(Password);

    public bool HasCredentials =>
        !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Password);

    /// <summary>
    /// Fills official Gmail/Resend/Brevo SMTP defaults when the provider is selected
    /// but host/username/encryption were left blank.
    /// </summary>
    public void ApplyKnownProviderDefaults()
    {
        if (IsGmail || Provider.Equals("Gmail", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(Provider))
            {
                Provider = "Gmail";
            }

            if (string.IsNullOrWhiteSpace(Host))
            {
                Host = GmailHost;
            }

            if (Port <= 0)
            {
                Port = 587;
            }

            if (Port is 465 or 2465)
            {
                Encryption = "SslOnConnect";
            }
            else if (string.IsNullOrWhiteSpace(Encryption))
            {
                Encryption = "StartTls";
            }

            // Gmail SMTP username is the full mailbox; prefer FromEmail when Username was omitted.
            if (string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(FromEmail))
            {
                Username = FromEmail.Trim();
            }

            if (string.IsNullOrWhiteSpace(FromEmail) && !string.IsNullOrWhiteSpace(Username)
                && Username.Contains('@', StringComparison.Ordinal))
            {
                FromEmail = Username.Trim();
            }
        }

        if (IsResend || Provider.Equals("Resend", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(Provider))
            {
                Provider = "Resend";
            }

            if (string.IsNullOrWhiteSpace(Host))
            {
                Host = ResendHost;
            }

            if (string.IsNullOrWhiteSpace(Username))
            {
                Username = ResendUsername;
            }

            if (Port is 465 or 2465)
            {
                Encryption = "SslOnConnect";
            }

            if (Port <= 0)
            {
                Port = 465;
                Encryption = "SslOnConnect";
            }
        }

        if (IsBrevo || Provider.Equals("Brevo", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(Provider))
            {
                Provider = "Brevo";
            }

            if (string.IsNullOrWhiteSpace(Host))
            {
                Host = "smtp-relay.brevo.com";
            }

            if (Port <= 0)
            {
                Port = 587;
            }

            if (string.IsNullOrWhiteSpace(Encryption))
            {
                Encryption = "StartTls";
            }
        }
    }

    public IReadOnlyList<string> DescribeMissingConfiguration()
    {
        ApplyKnownProviderDefaults();
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(Host))
        {
            missing.Add(nameof(Host));
        }

        if (Port is <= 0 or > 65535)
        {
            missing.Add(nameof(Port));
        }

        if (string.IsNullOrWhiteSpace(FromEmail))
        {
            missing.Add(nameof(FromEmail));
        }

        if (RequiresAuthentication)
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                missing.Add(nameof(Username));
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                missing.Add(nameof(Password));
            }
        }

        return missing;
    }
}
