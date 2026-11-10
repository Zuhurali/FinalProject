namespace FinalProject.MVC.Commerce;

/// <summary>Resolves SQL-backed category icon paths with a safe Home-strip fallback.</summary>
public static class CategoryIconPaths
{
    public const string AssetFolderWeb = "~/assets/images/category/";
    public const string AssetFolderRelative = "/assets/images/category/";

    public static string ForIndex(int index) =>
        $"{AssetFolderWeb}{(Math.Abs(index) % 6) + 1:00}.svg";

    /// <summary>
    /// Uses the stored SQL path when it is a safe local category asset; otherwise falls back
    /// to the previous HomeController rotation so the strip still renders.
    /// </summary>
    public static string Resolve(string? storedPath, int fallbackIndex)
    {
        if (IsSafeCategoryIcon(storedPath))
        {
            var text = storedPath!.Trim();
            return text.StartsWith("/", StringComparison.Ordinal) ? $"~{text}" : text;
        }

        return ForIndex(fallbackIndex);
    }

    public static bool IsSafeCategoryIcon(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var text = path.Trim();
        if (text.Length > 500
            || text.Contains("..", StringComparison.Ordinal)
            || text.Contains('\\'))
        {
            return false;
        }

        if (!(text.StartsWith("~/", StringComparison.Ordinal) || text.StartsWith("/", StringComparison.Ordinal))
            || text.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        var normalized = text.StartsWith("~/", StringComparison.Ordinal) ? text[1..] : text;
        if (!normalized.StartsWith(AssetFolderRelative, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var extension = Path.GetExtension(normalized);
        return extension.Equals(".svg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }
}
