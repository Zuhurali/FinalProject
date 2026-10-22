using System.Text.RegularExpressions;

namespace FinalProject.Commerce;

/// <summary>
/// Shared helpers for buyer-to-dealer car messaging (no DB access).
/// </summary>
public static class MarketplaceMessagingRules
{
    private static readonly Regex GeneratedListingLine = new(
        @"^(?:Listing:\s*)?(?:https?://\S+?)?/Cars/Details/\d+\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Removes auto-inserted listing paths such as "Listing: /Cars/Details/12"
    /// and the generated footer that only existed to carry that path.
    /// Car name and the buyer's own text are left intact. Stored rows are not changed.
    /// </summary>
    public static string StripGeneratedListingText(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var kept = new List<string>(lines.Length);
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (GeneratedListingLine.IsMatch(trimmed))
            {
                continue;
            }

            if (trimmed == "---" && IsGeneratedListingFooter(lines, i))
            {
                i += 2;
                continue;
            }

            kept.Add(lines[i].TrimEnd());
        }

        var text = string.Join("\n", kept).Trim();
        while (text.Contains("\n\n\n", StringComparison.Ordinal))
        {
            text = text.Replace("\n\n\n", "\n\n", StringComparison.Ordinal);
        }

        return text;
    }

    public static string DefaultSubject(string carName) =>
        $"Inquiry about {carName.Trim()}";

    private static bool IsGeneratedListingFooter(string[] lines, int separatorIndex)
    {
        var regardingIndex = separatorIndex + 1;
        var listingIndex = separatorIndex + 2;
        return regardingIndex < lines.Length
            && listingIndex < lines.Length
            && lines[regardingIndex].Trim().StartsWith("Regarding:", StringComparison.OrdinalIgnoreCase)
            && GeneratedListingLine.IsMatch(lines[listingIndex].Trim());
    }
}
