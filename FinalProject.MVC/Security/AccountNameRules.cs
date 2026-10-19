using System.Text.RegularExpressions;

namespace FinalProject.MVC.Security;

public static class AccountNameRules
{
    public const int MinLength = 3;
    public const int MaxLength = 32;

    private static readonly Regex UsernamePattern = new(
        "^[A-Za-z0-9][A-Za-z0-9._-]{2,31}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string? ValidateUsername(string? username)
    {
        var value = username?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            return "Enter a username.";
        }

        if (!UsernamePattern.IsMatch(value))
        {
            return "Use 3 to 32 characters: letters, numbers, dots, underscores, or hyphens.";
        }

        return null;
    }
}
