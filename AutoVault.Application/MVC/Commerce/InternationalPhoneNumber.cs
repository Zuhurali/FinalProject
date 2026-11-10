using System.Text.RegularExpressions;

namespace FinalProject.MVC.Commerce;

/// <summary>
/// Country-aware phone normalization for customer forms.
/// Country picker + fixed national length (not one endless field).
/// </summary>
public static class InternationalPhoneNumber
{
    public const string ErrorMessage =
        "Enter a valid phone number for the selected country. Extra digits are not allowed.";

    public static readonly PhoneCountry Azerbaijan = Country("AZ", "Azerbaijan", "994", 9);
    public static readonly PhoneCountry Turkey = Country("TR", "Turkey", "90", 10);
    public static readonly PhoneCountry UnitedStates = Country("US", "United States", "1", 10);
    public static readonly PhoneCountry Other = new("OTHER", "Other / not listed", "", 0, AllowLocalLeadingZero: false);

    // Featured first, then common destinations AutoVault customers use, then Other.
    public static IReadOnlyList<PhoneCountry> Countries { get; } =
    [
        Azerbaijan,
        Turkey,
        UnitedStates,
        Country("RU", "Russia", "7", 10),
        Country("GB", "United Kingdom", "44", 10),
        Country("DE", "Germany", "49", 11),
        Country("FR", "France", "33", 9),
        Country("IT", "Italy", "39", 10),
        Country("ES", "Spain", "34", 9),
        Country("UA", "Ukraine", "380", 9),
        Country("GE", "Georgia", "995", 9),
        Country("IR", "Iran", "98", 10),
        Country("CN", "China", "86", 11),
        Country("IN", "India", "91", 10),
        Country("AE", "United Arab Emirates", "971", 9),
        Country("SA", "Saudi Arabia", "966", 9),
        Country("KZ", "Kazakhstan", "7", 10),
        Country("UZ", "Uzbekistan", "998", 9),
        Country("CA", "Canada", "1", 10),
        Country("PL", "Poland", "48", 9),
        Country("NL", "Netherlands", "31", 9),
        Country("BE", "Belgium", "32", 9),
        Country("AT", "Austria", "43", 10),
        Country("CH", "Switzerland", "41", 9),
        Country("SE", "Sweden", "46", 9),
        Country("NO", "Norway", "47", 8),
        Country("FI", "Finland", "358", 9),
        Country("DK", "Denmark", "45", 8),
        Country("PT", "Portugal", "351", 9),
        Country("GR", "Greece", "30", 10),
        Country("RO", "Romania", "40", 9),
        Country("BG", "Bulgaria", "359", 9),
        Country("IL", "Israel", "972", 9),
        Country("JP", "Japan", "81", 10),
        Country("KR", "South Korea", "82", 10),
        Country("AU", "Australia", "61", 9),
        Country("NZ", "New Zealand", "64", 8),
        Country("BR", "Brazil", "55", 11),
        Country("MX", "Mexico", "52", 10),
        Country("EG", "Egypt", "20", 10),
        Country("PK", "Pakistan", "92", 10),
        Country("BY", "Belarus", "375", 9),
        Country("MD", "Moldova", "373", 8),
        Country("TM", "Turkmenistan", "993", 8),
        Country("KG", "Kyrgyzstan", "996", 9),
        Country("TJ", "Tajikistan", "992", 9),
        Other
    ];

    public static PhoneCountry ResolveCountry(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return Azerbaijan;
        }

        var match = Countries.FirstOrDefault(country =>
            string.Equals(country.Code, countryCode.Trim(), StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrEmpty(match.Code) ? Azerbaijan : match;
    }

    public static bool TryNormalize(string? countryCode, string? phone, out string normalized)
    {
        normalized = string.Empty;
        var country = ResolveCountry(countryCode);
        if (string.IsNullOrWhiteSpace(phone))
        {
            return false;
        }

        var value = phone.Trim();
        if (value.Any(static c => !(char.IsDigit(c) || c is '+' or ' ' or '-' or '(' or ')')))
        {
            return false;
        }

        if (value.Contains('+', StringComparison.Ordinal)
            && (value[0] != '+' || value.Count(static c => c == '+') > 1))
        {
            return false;
        }

        var digits = DigitsOnly(value);
        if (digits.Length == 0)
        {
            return false;
        }

        if (country.Code == Other.Code)
        {
            if (!PhoneNumberValidation.IsValid(value))
            {
                return false;
            }

            normalized = value.StartsWith('+') ? "+" + digits : digits;
            return !(normalized.StartsWith('+') && normalized.Length < 8);
        }

        if (digits.StartsWith(country.DialCode, StringComparison.Ordinal)
            && digits.Length == country.DialCode.Length + country.NationalDigits)
        {
            normalized = "+" + digits;
            return true;
        }

        if (country.AllowLocalLeadingZero
            && digits.StartsWith('0')
            && digits.Length == country.NationalDigits + 1)
        {
            normalized = "+" + country.DialCode + digits[1..];
            return Regex.IsMatch(normalized, @"^\+\d{8,15}$", RegexOptions.CultureInvariant);
        }

        if (digits.Length == country.NationalDigits
            && !digits.StartsWith(country.DialCode, StringComparison.Ordinal))
        {
            normalized = "+" + country.DialCode + digits;
            return true;
        }

        return false;
    }

    private static PhoneCountry Country(string code, string name, string dialCode, int nationalDigits) =>
        new(code, name, dialCode, nationalDigits, AllowLocalLeadingZero: false);

    private static string DigitsOnly(string value) =>
        new(value.Where(char.IsDigit).ToArray());

    public readonly record struct PhoneCountry(
        string Code,
        string Name,
        string DialCode,
        int NationalDigits,
        bool AllowLocalLeadingZero)
    {
        public int MaxInputDigits =>
            Code == "OTHER" ? PhoneNumberValidation.MaxDigits : NationalDigits;

        public string PrefixDisplay =>
            string.IsNullOrEmpty(DialCode) ? "+" : "+" + DialCode;

        public string OptionLabel =>
            Code == "OTHER" ? "Other / not listed" : $"{Name} (+{DialCode})";

        public string Hint =>
            Code == "OTHER"
                ? "Other: up to 15 digits total, include the country code."
                : $"{Name}: after +{DialCode} enter {NationalDigits} digits (no leading 0).";
    }
}
