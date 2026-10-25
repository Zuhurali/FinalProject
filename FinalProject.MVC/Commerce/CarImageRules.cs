namespace FinalProject.MVC.Commerce;

public static class CarImageRules
{
    public const int MaxBytes = 5 * 1024 * 1024;
    public const int MaxGalleryFilesPerSave = 12;
    public const string WebRelativeFolder = "~/assets/images/portfolio/cars/uploads";

    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    public static string? Validate(string? fileName, long length)
    {
        if (string.IsNullOrWhiteSpace(fileName) || length <= 0)
        {
            return "Choose an image file.";
        }

        if (length > MaxBytes)
        {
            return "The image must be 5 MB or smaller.";
        }

        var extension = Path.GetExtension(fileName);
        if (!Extensions.Contains(extension))
        {
            return "Use a JPG, JPEG, PNG, or WEBP image.";
        }

        return null;
    }

    public static bool IsAllowedExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        return Extensions.Contains(Path.GetExtension(fileName));
    }

    public static string BuildRelativePath(string storedFileName) =>
        $"{WebRelativeFolder}/{storedFileName}";

    public static bool TryResolveUpload(string? url, string webRootPath, out string physicalPath)
    {
        physicalPath = string.Empty;
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(webRootPath))
        {
            return false;
        }

        var text = url.Trim().Replace('\\', '/');
        const string marker = "assets/images/portfolio/cars/uploads/";
        var index = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return false;
        }

        var name = text[(index + marker.Length)..];
        if (name.Length == 0 || name.Contains('/') || name.Contains("..", StringComparison.Ordinal) || !IsAllowedExtension(name))
        {
            return false;
        }

        var baseName = Path.GetFileNameWithoutExtension(name);
        if (baseName.Length != 32 || baseName.Any(character => !Uri.IsHexDigit(character)))
        {
            return false;
        }

        var root = Path.GetFullPath(Path.Combine(webRootPath, "assets", "images", "portfolio", "cars", "uploads"));
        var candidate = Path.GetFullPath(Path.Combine(root, name));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!candidate.StartsWith(root + Path.DirectorySeparatorChar, comparison))
        {
            return false;
        }

        physicalPath = candidate;
        return true;
    }
}
