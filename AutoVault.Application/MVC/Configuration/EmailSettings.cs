namespace FinalProject.MVC.Configuration;

/// <summary>
/// SMTP settings bound from the EmailSettings configuration section.
/// Provide Host, Port, FromEmail, and usually Username/Password through User Secrets
/// or environment variables. Do not commit real credentials.
/// </summary>
public sealed class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "AutoVault";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host)
        && Port is > 0 and <= 65535
        && !string.IsNullOrWhiteSpace(FromEmail);
}
