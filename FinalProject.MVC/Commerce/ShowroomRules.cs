namespace FinalProject.MVC.Commerce;

public static class ShowroomRules
{
    public const int LowStockMaximum = 10;

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

        return quantity <= LowStockMaximum ? "Low Stock" : "In Stock";
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
        var windowError = ValidateCampaignWindowAndPercent(discountPercent, discountedPrice, startsAt, endsAt);
        if (windowError != null)
        {
            return windowError;
        }

        if (originalPrice <= 0)
        {
            return "The original price must be greater than zero.";
        }

        if (discountedPrice is decimal salePrice && (salePrice <= 0 || salePrice >= originalPrice))
        {
            return "The discounted price must be greater than zero and lower than the original price.";
        }

        return null;
    }

    public static string? ValidateCampaignWindowAndPercent(
        decimal? discountPercent,
        decimal? discountedPrice,
        DateTime startsAt,
        DateTime endsAt)
    {
        var hasPrice = discountedPrice.HasValue;
        var hasPercent = discountPercent.HasValue;
        if (hasPrice == hasPercent)
        {
            return "Enter either a discounted price or a discount percentage.";
        }

        if (discountPercent is <= 0 or > 100)
        {
            return "The discount percentage must be greater than 0 and at most 100.";
        }

        if (endsAt <= startsAt)
        {
            return "The end date must be after the start date.";
        }

        return null;
    }

    public static bool CampaignsConflict(
        bool aAppliesToAllParts,
        int? aProductId,
        bool bAppliesToAllParts,
        int? bProductId)
    {
        // All-parts + single-product can both stay active: the product campaign
        // takes precedence for that part, and the all-parts campaign covers the rest.
        if (aAppliesToAllParts && bAppliesToAllParts)
        {
            return true;
        }

        if (aAppliesToAllParts || bAppliesToAllParts)
        {
            return false;
        }

        return aProductId.HasValue
            && bProductId.HasValue
            && aProductId.Value == bProductId.Value;
    }

    public static bool OfferIsVisible(bool isActive, DateTime startsAt, DateTime endsAt, DateTime utcNow)
    {
        return isActive && utcNow >= startsAt && utcNow < endsAt;
    }

    public const string AllPartsCampaignImagePath = "~/assets/images/cta/01.webp";

    public static string CampaignBannerImagePath(string? productImagePath, bool appliesToAllParts)
    {
        if (!appliesToAllParts && !string.IsNullOrWhiteSpace(productImagePath))
        {
            return productImagePath;
        }

        return AllPartsCampaignImagePath;
    }

    public static bool CampaignWindowsOverlap(DateTime aStartsAt, DateTime aEndsAt, DateTime bStartsAt, DateTime bEndsAt)
    {
        return aStartsAt < bEndsAt && bStartsAt < aEndsAt;
    }

    public static string CampaignPublicStatus(bool isActive, DateTime startsAt, DateTime endsAt, DateTime utcNow)
    {
        if (!isActive)
        {
            return "Inactive";
        }

        if (utcNow < startsAt)
        {
            return "Scheduled";
        }

        if (utcNow >= endsAt)
        {
            return "Expired";
        }

        return "Live";
    }

    public static DateTime InclusiveEndUtc(DateTime exclusiveEndsAt)
    {
        return DateTime.SpecifyKind(exclusiveEndsAt.Date, DateTimeKind.Utc).AddDays(-1);
    }

    public static string InclusiveEndLabel(DateTime exclusiveEndsAt)
    {
        return InclusiveEndUtc(exclusiveEndsAt).ToString("MMM d", System.Globalization.CultureInfo.InvariantCulture);
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
