namespace FinalProject.Commerce;

public static class ShowroomApiRules
{
    public const string StatusNew = "New";
    public const string StatusContacted = "Contacted";
    public const string StatusCompleted = "Completed";
    public const string StatusCancelled = "Cancelled";

    public static readonly IReadOnlyList<string> AllStatuses =
    [
        StatusNew,
        StatusContacted,
        StatusCompleted,
        StatusCancelled
    ];

    public static bool IsOpen(string status) => status is StatusNew or StatusContacted;

    public static bool CanTransition(string? fromStatus, string? toStatus)
    {
        if (string.IsNullOrWhiteSpace(fromStatus) || string.IsNullOrWhiteSpace(toStatus))
        {
            return false;
        }

        if (!AllStatuses.Contains(toStatus))
        {
            return false;
        }

        if (string.Equals(fromStatus, toStatus, StringComparison.Ordinal))
        {
            return true;
        }

        return fromStatus switch
        {
            StatusNew => toStatus is StatusContacted or StatusCompleted or StatusCancelled,
            StatusContacted => toStatus is StatusCompleted or StatusCancelled,
            _ => false
        };
    }

    public static string? ValidateSaleQuantity(int requested, int available)
    {
        if (requested < 1)
        {
            return "Enter a quantity of at least 1.";
        }

        if (available < 0)
        {
            return "Available quantity cannot be negative.";
        }

        if (requested > available)
        {
            return "That sale is larger than the available quantity.";
        }

        return null;
    }

    public static decimal DiscountedPrice(decimal originalPrice, decimal? discountedPrice, decimal? discountPercent)
    {
        if (discountedPrice.HasValue)
        {
            return decimal.Round(discountedPrice.Value, 2, MidpointRounding.AwayFromZero);
        }

        var percent = discountPercent ?? 0;
        return decimal.Round(originalPrice * (1m - percent / 100m), 2, MidpointRounding.AwayFromZero);
    }

    public static int? BadgePercent(decimal originalPrice, decimal salePrice, decimal? discountPercent)
    {
        if (discountPercent.HasValue)
        {
            return (int)decimal.Round(discountPercent.Value, 0, MidpointRounding.AwayFromZero);
        }

        if (originalPrice <= 0 || salePrice >= originalPrice)
        {
            return null;
        }

        return (int)decimal.Round((1m - salePrice / originalPrice) * 100m, 0, MidpointRounding.AwayFromZero);
    }
}
