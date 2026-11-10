using System.Net.Mail;
using FinalProject.MVC.Models;

namespace FinalProject.MVC.Commerce;

public static class ProductInquiryRules
{
    public const int MinMessageLength = 10;
    public const int MaxMessageLength = 1000;

    public static string? Validate(ProductInquiry inquiry)
    {
        inquiry.CustomerName = (inquiry.CustomerName ?? string.Empty).Trim();
        inquiry.Email = (inquiry.Email ?? string.Empty).Trim();
        inquiry.Phone = string.IsNullOrWhiteSpace(inquiry.Phone) ? null : inquiry.Phone.Trim();
        inquiry.Message = (inquiry.Message ?? string.Empty).Trim();

        if (inquiry.ProductId <= 0)
        {
            return "Choose a product before sending a question.";
        }

        if (inquiry.CustomerName.Length is < 2 or > 80)
        {
            return "Enter your name.";
        }

        if (inquiry.Email.Length > 200 || !IsEmail(inquiry.Email))
        {
            return "Enter a valid email address.";
        }

        if (inquiry.Phone != null && inquiry.Phone.Length > 40)
        {
            return "Enter a shorter phone number.";
        }

        if (inquiry.Message.Length < MinMessageLength || inquiry.Message.Length > MaxMessageLength)
        {
            return $"Write between {MinMessageLength} and {MaxMessageLength} characters.";
        }

        return null;
    }

    private static bool IsEmail(string value)
    {
        try
        {
            _ = new MailAddress(value);
            return value.Contains('@');
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
