namespace FinalProject.MVC.Commerce;

public static class PartImageRules
{
    public const int MaxBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    public static string? Validate(string fileName, long length)
    {
        if (length <= 0)
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
            return "Use a JPG, PNG, WEBP, or GIF image.";
        }

        return null;
    }
}
