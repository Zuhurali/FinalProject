namespace FinalProject.Security;

/// <summary>
/// Resolves the API JWT signing key from configuration (user secrets / environment).
/// Never logs or returns guidance that includes the secret value.
/// </summary>
public static class ApiSigningKey
{
    public const string ConfigurationKey = "ApiAuthentication:SigningKey";
    public const string EnvironmentVariable = "ApiAuthentication__SigningKey";
    public const int MinimumLength = 32;

    public static string RequireConfigured(IConfiguration configuration)
    {
        var signingKey = configuration[ConfigurationKey];
        if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < MinimumLength)
        {
            throw new InvalidOperationException(
                $"{ConfigurationKey} must be configured with at least {MinimumLength} characters. "
                + $"Set it via .NET user secrets for local development, or the {EnvironmentVariable} environment variable / secret store for deployment. "
                + "Do not commit the signing key to source control.");
        }

        return signingKey;
    }
}
