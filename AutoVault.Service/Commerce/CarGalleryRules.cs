namespace FinalProject.Commerce;

public static class CarGalleryRules
{
    public const int MaxImages = 24;

    public const int MaxNewImagesPerRequest = 12;

    /// <summary>
    /// Seeded catalog assets and Admin/account uploads under wwwroot.
    /// Proven against FinalProjectDb CarImages (catalog + portfolio/cars/uploads only).
    /// </summary>
    private static readonly string[] AllowedPathPrefixes =
    [
        "/assets/images/catalog/",
        "/assets/images/portfolio/cars/uploads/"
    ];

    public static bool UrlsMatch(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    public static bool IsSafeReference(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var text = url.Trim();
        if (text.Length > 500
            || text.Contains("..", StringComparison.Ordinal)
            || text.Contains('\\'))
        {
            return false;
        }

        // Local app-relative paths only. Reject http(s) and protocol-relative "//…".
        if (!(text.StartsWith("~/", StringComparison.Ordinal) || text.StartsWith("/", StringComparison.Ordinal))
            || text.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        var normalized = text.StartsWith("~/", StringComparison.Ordinal) ? text[1..] : text;
        return AllowedPathPrefixes.Any(prefix =>
            normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class CarListingException : Exception
{
    public CarListingException(string message)
        : base(message)
    {
    }
}

public sealed class CarGalleryException : Exception
{
    public CarGalleryException(string message)
        : base(message)
    {
    }
}
