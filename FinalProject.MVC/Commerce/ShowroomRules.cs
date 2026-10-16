namespace FinalProject.MVC.Commerce;

public static class ShowroomRules
{
    public static string StockStatus(int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");
        }

        if (quantity == 0)
        {
            return "Out of Stock";
        }

        return quantity <= 7 ? "Low Stock" : "In Stock";
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

    public static string? ValidateCampaign(
        decimal originalPrice,
        decimal? discountedPrice,
        decimal? discountPercent,
        DateTime startsAt,
        DateTime endsAt)
    {
        if (originalPrice <= 0)
        {
            return "The original price must be greater than zero.";
        }

        var hasPrice = discountedPrice.HasValue;
        var hasPercent = discountPercent.HasValue;
        if (hasPrice == hasPercent)
        {
            return "Enter either a discounted price or a discount percentage.";
        }

        if (discountedPrice is <= 0 || discountedPrice >= originalPrice)
        {
            return "The discounted price must be greater than zero and lower than the original price.";
        }

        if (discountPercent is < 1 or > 90)
        {
            return "The discount percentage must be from 1 to 90.";
        }

        if (endsAt <= startsAt)
        {
            return "The end date must be after the start date.";
        }

        return null;
    }

    public static bool OfferIsVisible(bool isActive, DateTime startsAt, DateTime endsAt, DateTime utcNow)
    {
        return isActive && utcNow >= startsAt && utcNow < endsAt;
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
