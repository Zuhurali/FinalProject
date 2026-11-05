using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace FinalProject.MVC.Commerce;

public static class AzerbaijanPhoneNumber
{
    public const string ErrorMessage = "Enter an Azerbaijani mobile number as 0503546767 or +994503546767. Extra digits, letters, and other characters are not allowed.";

    private static readonly Regex LocalMobile = new(@"^0[0-9]{9}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex InternationalMobile = new(@"^\+994[0-9]{9}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool TryNormalize(string? phone, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(phone))
        {
            return false;
        }

        var value = phone.Trim();
        if (InternationalMobile.IsMatch(value))
        {
            normalized = value;
            return true;
        }

        if (!LocalMobile.IsMatch(value))
        {
            return false;
        }

        normalized = "+994" + value[1..];
        return InternationalMobile.IsMatch(normalized);
    }
}

public sealed class AzerbaijanPhoneAttribute : ValidationAttribute
{
    public AzerbaijanPhoneAttribute()
    {
        ErrorMessage = AzerbaijanPhoneNumber.ErrorMessage;
    }

    public override bool IsValid(object? value) =>
        value is string phone && AzerbaijanPhoneNumber.TryNormalize(phone, out _);
}
