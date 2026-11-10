using System.Text.RegularExpressions;

namespace FinalProject.MVC.Services;

/// <summary>
/// Prepares a development email for an admin screen without printing confirmation or reset tokens.
/// The stored message is unchanged so a signed-in admin can still open the real link.
/// </summary>
public static partial class DevelopmentMailDisplay
{
    public static string RedactSensitiveLinks(string? body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return string.Empty;
        }

        return UrlPattern().Replace(body, match =>
            IsSensitive(match.Value) ? "[secure link hidden]" : match.Value);
    }

    public static IReadOnlyList<string> SensitiveLinks(string? body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return Array.Empty<string>();
        }

        return UrlPattern()
            .Matches(body)
            .Select(match => match.Value.TrimEnd('.', ',', ';'))
            .Where(IsSensitive)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public static bool CanFollow(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && string.IsNullOrEmpty(uri.UserInfo)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool IsSensitive(string url)
    {
        return url.Contains("ConfirmEmail", StringComparison.OrdinalIgnoreCase)
            || url.Contains("ResetPassword", StringComparison.OrdinalIgnoreCase)
            || url.Contains("code=", StringComparison.OrdinalIgnoreCase)
            || url.Contains("token=", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"https?://[^\s<>""']+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UrlPattern();
}
