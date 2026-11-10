using System.Net.Mail;
using FinalProject.MVC.Models;

namespace FinalProject.MVC.Commerce;

public static class ContactFormRules
{
    public const int MinMessageLength = 10;
    public const int MaxMessageLength = 1000;

    /// <summary>
    /// Trims fields and returns field-keyed validation errors. Empty dictionary means valid.
    /// </summary>
    public static Dictionary<string, string> Validate(ContactFormModel model)
    {
        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        model.Name = (model.Name ?? string.Empty).Trim();
        model.Email = (model.Email ?? string.Empty).Trim();
        model.Phone = (model.Phone ?? string.Empty).Trim();
        model.Car = (model.Car ?? string.Empty).Trim();
        model.Message = (model.Message ?? string.Empty).Trim();

        if (model.Name.Length is < 2 or > 80)
        {
            errors["name"] = "Enter your name.";
        }

        if (model.Email.Length is 0 or > 200 || !IsEmail(model.Email))
        {
            errors["email"] = "Enter a valid email address.";
        }

        if (!InternationalPhoneNumber.TryNormalize(model.PhoneCountry, model.Phone, out var normalizedPhone))
        {
            errors["phone"] = InternationalPhoneNumber.ErrorMessage;
        }
        else
        {
            model.Phone = normalizedPhone;
        }

        if (model.Car.Length is < 2 or > 120)
        {
            errors["car"] = "Tell us which part or car you are interested in.";
        }

        if (model.Message.Length < MinMessageLength || model.Message.Length > MaxMessageLength)
        {
            errors["message"] = $"Write between {MinMessageLength} and {MaxMessageLength} characters.";
        }

        return errors;
    }

    private static bool IsEmail(string value)
    {
        try
        {
            _ = new MailAddress(value);
            return value.Contains('@', StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
