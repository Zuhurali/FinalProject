using System.Net.Mail;
using FinalProject.MVC.Models;

namespace FinalProject.MVC.Commerce;

public static class ShowroomRequestRules
{
    public const int MaxQuantityPerRequest = 10;
    public const int MaxVisitDaysAhead = 60;
    public const int MaxNoteLength = 500;

    /// <summary>
    /// Allowed admin status transitions for showroom purchase requests.
    /// Completed is only set via CompletePurchaseRequestWithSaleAsync (never via status PATCH).
    /// Cancelled is terminal.
    /// </summary>
    public static bool CanTransition(string? fromStatus, string? toStatus)
    {
        if (string.IsNullOrWhiteSpace(fromStatus) || string.IsNullOrWhiteSpace(toStatus))
        {
            return false;
        }

        if (!ShowroomRequestStatus.All.Contains(toStatus))
        {
            return false;
        }

        if (string.Equals(fromStatus, toStatus, StringComparison.Ordinal))
        {
            return true;
        }

        // Completed requires a linked stock-moving sale — refuse status-only completion.
        if (string.Equals(toStatus, ShowroomRequestStatus.Completed, StringComparison.Ordinal))
        {
            return false;
        }

        return fromStatus switch
        {
            ShowroomRequestStatus.New => toStatus is ShowroomRequestStatus.Contacted
                or ShowroomRequestStatus.Cancelled,
            ShowroomRequestStatus.Contacted => toStatus is ShowroomRequestStatus.Cancelled,
            _ => false
        };
    }

    public static IReadOnlyList<string> AllowedNextStatuses(string? fromStatus)
    {
        if (string.IsNullOrWhiteSpace(fromStatus) || !ShowroomRequestStatus.All.Contains(fromStatus))
        {
            return Array.Empty<string>();
        }

        // Completed / Cancelled are terminal: admin UI shows a static label, not a dropdown.
        // Completing a request (and reducing stock) is only via CompletePurchaseRequestWithSaleAsync.
        if (fromStatus is ShowroomRequestStatus.Completed or ShowroomRequestStatus.Cancelled)
        {
            return Array.Empty<string>();
        }

        // New requests: stay New or cancel. Do not offer Completed here (sale workflow handles that).
        if (string.Equals(fromStatus, ShowroomRequestStatus.New, StringComparison.Ordinal))
        {
            return [ShowroomRequestStatus.New, ShowroomRequestStatus.Cancelled];
        }

        // Legacy Contacted open requests: stay Contacted or cancel.
        if (string.Equals(fromStatus, ShowroomRequestStatus.Contacted, StringComparison.Ordinal))
        {
            return [ShowroomRequestStatus.Contacted, ShowroomRequestStatus.Cancelled];
        }

        return Array.Empty<string>();
    }

    public static string? Validate(
        ShowroomPurchaseRequest request,
        int availableQuantity,
        DateTime todayLocal,
        string? phoneCountry = null)
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
            return availableQuantity <= 0
                ? "All current stock is already covered by open purchase requests. Try again later or call the store."
                : $"Only {availableQuantity} available after open requests. Lower the quantity and try again.";
        }

        if (request.CustomerName.Length is < 2 or > 80)
        {
            return "Enter your name.";
        }

        if (request.Email.Length > 200 || !IsEmail(request.Email))
        {
            return "Enter a valid email address.";
        }

        if (!InternationalPhoneNumber.TryNormalize(phoneCountry, request.Phone, out var normalizedPhone))
        {
            return InternationalPhoneNumber.ErrorMessage;
        }

        request.Phone = normalizedPhone;

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

    /// <summary>
    /// Units still free for new requests after open New/Contacted reservations.
    /// </summary>
    public static int UnreservedQuantity(int onHandQuantity, int openReservedQuantity) =>
        Math.Max(0, onHandQuantity - Math.Max(0, openReservedQuantity));

    /// <summary>
    /// Walk-in sales must leave enough stock for existing open purchase requests.
    /// </summary>
    public static string? ValidateSaleAgainstReservations(int saleQuantity, int onHandQuantity, int openReservedQuantity)
    {
        var stockError = ShowroomRules.ValidateSaleQuantity(saleQuantity, onHandQuantity);
        if (stockError != null)
        {
            return stockError;
        }

        var remaining = onHandQuantity - saleQuantity;
        if (remaining < openReservedQuantity)
        {
            return openReservedQuantity == 0
                ? null
                : $"That sale would leave only {remaining} in stock, but {openReservedQuantity} unit(s) are reserved by open purchase requests.";
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
