namespace FinalProject.MVC.Commerce;

public static class SiteBrandAssetRules
{
    public const int MaxBytes = 2 * 1024 * 1024;
    public const string UploadFolderWeb = "~/assets/images/site/uploads";
    public const string UploadFolderRelative = "assets/images/site/uploads";

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".svg", ".png", ".jpg", ".jpeg", ".webp", ".ico"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/svg+xml",
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/x-icon",
        "image/vnd.microsoft.icon"
    };

    public static string? ValidateUpload(string? fileName, string? contentType, long length)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "Choose an image file.";
        }

        if (length <= 0)
        {
            return "The image file is empty.";
        }

        if (length > MaxBytes)
        {
            return "Images must be 2 MB or smaller.";
        }

        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return "Use an SVG, PNG, JPG, WEBP, or ICO image.";
        }

        if (!string.IsNullOrWhiteSpace(contentType) && !AllowedContentTypes.Contains(contentType))
        {
            return "That image type is not allowed.";
        }

        return null;
    }

    public static bool IsSafeStoredPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var text = path.Trim().Replace('\\', '/');
        if (text.Contains("..", StringComparison.Ordinal) || text.Length > 500)
        {
            return false;
        }

        if (!(text.StartsWith("~/", StringComparison.Ordinal) || text.StartsWith("/", StringComparison.Ordinal)))
        {
            return false;
        }

        var normalized = text.StartsWith("~/", StringComparison.Ordinal) ? text[1..] : text;
        return normalized.StartsWith("/assets/images/logo/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("/assets/images/site/", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "/fav.svg", StringComparison.OrdinalIgnoreCase);
    }

    public static string? ValidateStoredPath(string? path) =>
        IsSafeStoredPath(path) ? null : "That brand image path is not allowed.";
}
