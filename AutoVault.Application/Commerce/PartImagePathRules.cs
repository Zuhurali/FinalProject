namespace FinalProject.Commerce;

/// <summary>
/// Allowlisted relative image paths for spare parts. Matches seeded portfolio paths
/// and MVC uploads under parts/uploads. Rejects traversal and arbitrary file paths.
/// </summary>
public static class PartImagePathRules
{
    public const int MaxLength = 500;

    public static bool IsSafeReference(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var text = path.Trim().Replace('\\', '/');
        if (text.Contains("..", StringComparison.Ordinal) || text.Length > MaxLength)
        {
            return false;
        }

        if (text.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            return Uri.TryCreate(text, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                && string.IsNullOrEmpty(uri.UserInfo);
        }

        if (!(text.StartsWith("~/", StringComparison.Ordinal) || text.StartsWith("/", StringComparison.Ordinal)))
        {
            return false;
        }

        var normalized = text.StartsWith("~/", StringComparison.Ordinal) ? text[1..] : text;
        return normalized.StartsWith("/assets/images/portfolio/", StringComparison.OrdinalIgnoreCase);
    }

    public static string? Validate(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "Add a product image.";
        }

        return IsSafeReference(path) ? null : "The product image path is not allowed.";
    }
}
