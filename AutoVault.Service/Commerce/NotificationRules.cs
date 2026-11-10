namespace FinalProject.Commerce;

public static class NotificationRules
{
    public const string MessageType = "message";
    public const string PriceDropType = "price_drop";
    public const string ListingType = "listing";

    public const string MenuMessages = "messages";
    public const string MenuFavorites = "favorites";
    public const string MenuListings = "listings";
    public const string MenuOther = "other";

    public static bool IsPriceDrop(decimal previousPrice, decimal newPrice) =>
        newPrice < previousPrice;

    public static string MenuCategory(string type) =>
        type switch
        {
            MessageType => MenuMessages,
            PriceDropType => MenuFavorites,
            ListingType => MenuListings,
            _ => MenuOther
        };

    public static string MessageDedupKey(int messageId) => $"message:{messageId}";

    public static string PriceDropDedupKey(int carId, decimal previousPrice, decimal newPrice) =>
        $"price_drop:{carId}:{previousPrice:0.##}:{newPrice:0.##}";

    public static string FormatPriceDropBody(
        string carName,
        decimal previousPrice,
        decimal newPrice)
    {
        var saved = previousPrice - newPrice;
        var percent = previousPrice <= 0
            ? 0m
            : Math.Round(saved / previousPrice * 100m, 1);
        return $"Special offer! {carName} price dropped from ${previousPrice:N0} to ${newPrice:N0} (save ${saved:N0}, {percent}%).";
    }

    public static string FormatMessageBody(string senderDisplayName, string preview)
    {
        var clean = preview.Trim();
        if (clean.Length > 120)
        {
            clean = clean[..117] + "...";
        }

        return $"{senderDisplayName}: {clean}";
    }
}
