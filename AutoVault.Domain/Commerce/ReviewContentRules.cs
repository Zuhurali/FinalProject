namespace FinalProject.Commerce;

public static class ReviewContentRules
{
    /// <summary>Public label for reviews whose author soft-deleted their Member account.</summary>
    public const string DeletedAccountDisplayName = "Deleted Account";

    public const int MinLength = 1;
    public const int MaxLength = 1000;

    /// <summary>
    /// Maximum number of times the same token may appear consecutively before the review
    /// is treated as spam. E.g. "ser ser ser ser ser" with a threshold of 5 is rejected.
    /// </summary>
    public const int MaxConsecutiveRepeats = 5;

    public static string? Normalize(string? content, out string normalized)
    {
        normalized = (content ?? string.Empty).Trim();

        if (normalized.Length < MinLength)
        {
            return "Write at least 1 character.";
        }

        if (normalized.Length > MaxLength)
        {
            return $"Reviews cannot be longer than {MaxLength} characters.";
        }

        var spamError = CheckForRepeatedTokenSpam(normalized);
        if (spamError != null)
        {
            return spamError;
        }

        return null;
    }

    /// <summary>
    /// Returns an error message when the same word appears
    /// <see cref="MaxConsecutiveRepeats"/> or more times in a row
    /// (case-insensitive, whitespace-normalised).
    /// A single-word review or normal prose always passes.
    /// </summary>
    public static string? CheckForRepeatedTokenSpam(string text)
    {
        var tokens = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length < MaxConsecutiveRepeats)
        {
            return null;
        }

        var streak = 1;
        for (var i = 1; i < tokens.Length; i++)
        {
            if (string.Equals(tokens[i], tokens[i - 1], StringComparison.OrdinalIgnoreCase))
            {
                streak++;
                if (streak >= MaxConsecutiveRepeats)
                {
                    return "Your review looks like spam. Please write a genuine review.";
                }
            }
            else
            {
                streak = 1;
            }
        }

        return null;
    }
}
