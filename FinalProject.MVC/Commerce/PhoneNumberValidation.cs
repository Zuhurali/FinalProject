using System.Text.RegularExpressions;

namespace FinalProject.MVC.Commerce;

/// <summary>
/// Shared phone rules for customer-facing contact forms.
/// Digits required; optional leading +; spaces, hyphens, and parentheses allowed.
/// Letters and mixed alphanumeric values are rejected (not stripped).
/// </summary>
public static class PhoneNumberValidation
{
    public const string ErrorMessage = "Enter a valid phone number.";
    public const int MinDigits = 7;
    public const int MaxDigits = 15;
    public const int MaxFormattedLength = 25;

    // Optional +, then digits with common formatting; must contain only those characters.
    private static readonly Regex AllowedFormat = new(
        @"^\+?(?:[\s\-()]*\d){7,15}[\s\-()]*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool IsValid(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return false;
        }

        var value = phone.Trim();
        if (value.Length > MaxFormattedLength)
        {
            return false;
        }

        // Reject letters / other symbols without stripping them away.
        if (value.Any(static c => !(char.IsDigit(c) || c is '+' or ' ' or '-' or '(' or ')')))
        {
            return false;
        }

        if (value.Contains('+', StringComparison.Ordinal) &&
            (value[0] != '+' || value.Count(static c => c == '+') > 1))
        {
            return false;
        }

        var digits = value.Count(char.IsDigit);
        if (digits is < MinDigits or > MaxDigits)
        {
            return false;
        }

        return AllowedFormat.IsMatch(value);
    }
}
