namespace FinalProject.Commerce;

public static class ReviewContentRules
{
    public const int MinLength = 10;
    public const int MaxLength = 1000;

    public static string? Normalize(string? content, out string normalized)
    {
        normalized = (content ?? string.Empty).Trim();
        if (normalized.Length < MinLength)
        {
            return $"Write at least {MinLength} characters.";
        }

        if (normalized.Length > MaxLength)
        {
            return $"Reviews cannot be longer than {MaxLength} characters.";
        }

        return null;
    }
}
