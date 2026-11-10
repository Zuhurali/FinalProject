using FinalProject.MVC.Configuration;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FinalProject.MVC.Hosting;

public static class EmailDeliveryStartupDiagnostics
{
    public static void Log(IServiceProvider services, ILogger logger)
    {
        var settings = services.GetRequiredService<IOptions<EmailSettings>>().Value;
        settings.ApplyKnownProviderDefaults();
        var sender = services.GetRequiredService<IEmailSender>();

        if (settings.IsConfigured && sender.IsExternalConfigured)
        {
            logger.LogInformation(
                "External email provider is configured. Provider={Provider}, Host={Host}, Port={Port}, Encryption={Encryption}, FromEmail={FromEmail}, FromName={FromName}, Username={Username}, PasswordConfigured={PasswordConfigured}, DevelopmentMailboxCopy={MailboxCopy}",
                string.IsNullOrWhiteSpace(settings.Provider) ? "(unspecified)" : settings.Provider,
                settings.Host,
                settings.Port,
                settings.Encryption,
                settings.FromEmail,
                settings.FromName,
                string.IsNullOrWhiteSpace(settings.Username) ? "(none)" : settings.Username,
                settings.HasCredentials,
                services.GetService<IDevelopmentMailStore>() != null);
            return;
        }

        var missing = settings.DescribeMissingConfiguration();
        logger.LogWarning(
            "External email provider is NOT configured. Confirmation mail will use the Development Mailbox only in Development. Missing settings: {Missing}. Set User Secrets EmailSettings:* (see appsettings.Email.example.json).",
            missing.Count == 0 ? "(incomplete provider settings)" : string.Join(", ", missing));
    }
}
