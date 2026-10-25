namespace FinalProject.Commerce;

public static class CarGalleryRules
{
    public const int MaxImages = 24;

    public const int MaxNewImagesPerRequest = 12;

    public static bool UrlsMatch(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    public static bool IsSafeReference(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var text = url.Trim();
        if (text.Length > 500 || text.IndexOf("..", StringComparison.Ordinal) >= 0 || text.IndexOf('\\') >= 0)
        {
            return false;
        }

        return text.StartsWith("~/", StringComparison.Ordinal)
            || text.StartsWith("/", StringComparison.Ordinal)
            || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
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
