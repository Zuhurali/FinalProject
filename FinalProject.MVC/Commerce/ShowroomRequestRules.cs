using System.Net.Mail;
using FinalProject.MVC.Models;

namespace FinalProject.MVC.Commerce;

public static class ShowroomRequestRules
{
    public const int MaxQuantityPerRequest = 10;
    public const int MaxVisitDaysAhead = 60;
    public const int MaxNoteLength = 500;

    public static string? Validate(ShowroomPurchaseRequest request, int availableQuantity, DateTime todayLocal)
    {
        request.CustomerName = (request.CustomerName ?? string.Empty).Trim();
        request.Email = (request.Email ?? string.Empty).Trim();
        request.Phone = (request.Phone ?? string.Empty).Trim();
        request.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        if (request.ProductId <= 0)
        {
            return "Choose a product before sending a request.";
        }

        if (request.SubmissionKey == Guid.Empty)
        {
            return "The form expired. Reload the page and try again.";
        }

        if (availableQuantity <= 0)
        {
            return "This product is out of stock, so it cannot be requested. Ask a question or call the store instead.";
        }

        if (request.Quantity < 1 || request.Quantity > MaxQuantityPerRequest)
        {
            return $"Choose a quantity from 1 to {MaxQuantityPerRequest}.";
        }

        if (request.Quantity > availableQuantity)
        {
            return $"Only {availableQuantity} available. Lower the quantity and try again.";
        }

        if (request.CustomerName.Length is < 2 or > 80)
        {
            return "Enter your name.";
        }

        if (request.Email.Length > 200 || !IsEmail(request.Email))
        {
            return "Enter a valid email address.";
        }

        var digits = request.Phone.Count(char.IsDigit);
        if (request.Phone.Length > 40 || digits < 7)
        {
            return "Enter a phone number our team can call.";
        }

        if (request.PreferredVisitDate.HasValue)
        {
            var visit = request.PreferredVisitDate.Value.Date;
            if (visit < todayLocal.Date || visit > todayLocal.Date.AddDays(MaxVisitDaysAhead))
            {
                return $"Choose a visit date between today and {MaxVisitDaysAhead} days from now.";
            }

            request.PreferredVisitDate = visit;
        }

        if (request.Note is { Length: > MaxNoteLength })
        {
            return $"Keep the note under {MaxNoteLength} characters.";
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
