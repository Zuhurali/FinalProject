namespace FinalProject.MVC.Configuration;

/// <summary>
/// Public site address used when building links that leave the server, such as email confirmation.
/// </summary>
public sealed class ApplicationSettings
{
    public const string SectionName = "Application";

    /// <summary>
    /// Absolute public origin, for example https://autovault.example.
    /// Leave empty to use the current request host (typical for local development).
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}
