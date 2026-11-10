using System.Text;

namespace FinalProject.MVC.Configuration;

public sealed class DealershipContactSettings
{
    public const string SectionName = "DealershipContact";

    public string DisplayPhone { get; set; } = DealershipContactDefaults.DisplayPhone;

    /// <summary>E.164 phone used in tel: links (e.g. +994503546767).</summary>
    public string E164Phone { get; set; } = DealershipContactDefaults.E164Phone;

    /// <summary>Digits-only WhatsApp destination for wa.me (no +).</summary>
    public string WhatsAppDigits { get; set; } = DealershipContactDefaults.WhatsAppDigits;

    public string Email { get; set; } = DealershipContactDefaults.Email;

    public string AddressLine { get; set; } = DealershipContactDefaults.AddressLine;

    public string MapsQuery { get; set; } = DealershipContactDefaults.MapsQuery;

    /// <summary>Opens the device dialer only. Never used for WhatsApp.</summary>
    public string TelHref => $"tel:{NormalizeE164(E164Phone)}";

    /// <summary>
    /// Opens a WhatsApp chat (wa.me), not a voice call and not a tel: link.
    /// </summary>
    public string WhatsAppHref
    {
        get
        {
            var digits = NormalizeWhatsAppDigits(WhatsAppDigits, E164Phone);
            // Chat deep link only — do not append call= or voice parameters.
            return $"https://wa.me/{digits}";
        }
    }

    public string MailtoHref => $"mailto:{Email.Trim()}";

    public string MapsHref
    {
        get
        {
            var query = string.IsNullOrWhiteSpace(MapsQuery) ? AddressLine : MapsQuery;
            return "https://www.google.com/maps/search/?api=1&query="
                   + Uri.EscapeDataString(query.Trim());
        }
    }

    public static string NormalizeE164(string? value)
    {
        var raw = (value ?? string.Empty).Trim();
        if (raw.Length == 0)
        {
            return DealershipContactDefaults.E164Phone;
        }

        var digits = KeepDigits(raw);
        if (digits.Length == 0)
        {
            return DealershipContactDefaults.E164Phone;
        }

        if (digits.StartsWith("00", StringComparison.Ordinal))
        {
            return "+" + digits[2..];
        }

        return digits.StartsWith('+') ? digits : "+" + digits;
    }

    public static string NormalizeWhatsAppDigits(string? whatsAppDigits, string? e164Fallback)
    {
        var digits = KeepDigits(whatsAppDigits).TrimStart('+');
        if (digits.Length == 0)
        {
            digits = KeepDigits(e164Fallback).TrimStart('+');
        }

        return digits.Length == 0
            ? DealershipContactDefaults.WhatsAppDigits
            : digits;
    }

    /// <summary>
    /// Builds a dialable tel: href from any phone string without inventing a number.
    /// </summary>
    public static string TelHrefFor(string? phone) =>
        $"tel:{NormalizeE164(string.IsNullOrWhiteSpace(phone) ? DealershipContactDefaults.E164Phone : phone)}";

    private static string KeepDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.Trim())
        {
            if (ch == '+' && builder.Length == 0)
            {
                builder.Append(ch);
            }
            else if (char.IsDigit(ch))
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }
}
