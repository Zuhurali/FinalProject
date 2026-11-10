namespace FinalProject.MVC.Commerce;

public static class CmsAssetUpload
{
    public const int MaxBytes = 4 * 1024 * 1024;

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

    public static bool IsSafeStoredPath(string? path, IReadOnlyList<string> allowedPathPrefixes)
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
        return allowedPathPrefixes.Any(prefix =>
            normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    public static string CoalescePath(string? path, string fallback, IReadOnlyList<string> allowedPathPrefixes) =>
        IsSafeStoredPath(path, allowedPathPrefixes) ? path!.Trim() : fallback;

    public static async Task<(string? Path, string? Error)> TrySaveAsync(
        IWebHostEnvironment environment,
        string uploadFolderRelative,
        string uploadFolderWeb,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return (null, null);
        }

        var validation = ValidateUpload(file.FileName, file.ContentType, file.Length);
        if (validation != null)
        {
            return (null, validation);
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var folder = Path.Combine(environment.WebRootPath, uploadFolderRelative);
        Directory.CreateDirectory(folder);
        var physical = Path.Combine(folder, fileName);
        await using (var stream = File.Create(physical))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return ($"{uploadFolderWeb}/{fileName}", null);
    }
}
