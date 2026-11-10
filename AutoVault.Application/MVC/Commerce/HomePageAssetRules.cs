namespace FinalProject.MVC.Commerce;

public static class HomePageAssetRules
{
    public const int MaxBytes = 4 * 1024 * 1024;
    public const string UploadFolderWeb = "~/uploads/home";
    public const string UploadFolderRelative = "uploads/home";

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".svg", ".png", ".jpg", ".jpeg", ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/svg+xml",
        "image/png",
        "image/jpeg",
        "image/webp"
    };

    private static readonly string[] AllowedPathPrefixes =
    {
        "/uploads/home/",
        "/assets/images/catalog/hero/",
        "/assets/images/about/",
        "/assets/images/why-choose/",
        "/assets/images/category/",
        "/assets/images/brand/",
        "/assets/images/cta/",
        "/assets/images/portfolio/",
        "/assets/images/feature/",
        "/uploads/about/",
        "/uploads/team/",
        "/uploads/dealers/",
        "/uploads/shop/",
        "/assets/images/catalog/sports-car/",
        "/assets/images/catalog/luxury/",
        "/assets/images/team/",
        "/assets/images/catalog/hero/"
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
            return "Images must be 4 MB or smaller.";
        }

        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return "Use an SVG, PNG, JPG, or WEBP image.";
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
        return AllowedPathPrefixes.Any(prefix =>
            normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    public static string? ValidateStoredPath(string? path) =>
        IsSafeStoredPath(path) ? null : "That homepage image path is not allowed.";

    public static string CoalescePath(string? path, string fallback) =>
        IsSafeStoredPath(path) ? path!.Trim() : fallback;
}
