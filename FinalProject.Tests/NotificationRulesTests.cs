using FinalProject.Commerce;
using Xunit;

namespace FinalProject.Tests;

public class NotificationRulesTests
{
    [Fact]
    public void IsPriceDrop_only_when_new_price_is_lower()
    {
        Assert.True(NotificationRules.IsPriceDrop(42860m, 39900m));
        Assert.False(NotificationRules.IsPriceDrop(39900m, 39900m));
        Assert.False(NotificationRules.IsPriceDrop(39900m, 42860m));
        Assert.False(NotificationRules.IsPriceDrop(100m, 100m));
    }

    [Fact]
    public void FormatPriceDropBody_includes_car_and_prices()
    {
        var body = NotificationRules.FormatPriceDropBody("Audi A5", 42860m, 39900m);

        Assert.Contains("Special offer!", body);
        Assert.Contains("Audi A5", body);
        Assert.Contains("42,860", body);
        Assert.Contains("39,900", body);
    }

    [Fact]
    public void Dedup_keys_are_stable_for_same_inputs()
    {
        Assert.Equal("message:42", NotificationRules.MessageDedupKey(42));
        Assert.Equal(
            NotificationRules.PriceDropDedupKey(7, 100m, 90m),
            NotificationRules.PriceDropDedupKey(7, 100m, 90m));
        Assert.NotEqual(
            NotificationRules.PriceDropDedupKey(7, 100m, 90m),
            NotificationRules.PriceDropDedupKey(7, 100m, 80m));
    }

    [Fact]
    public void MenuCategory_maps_types_without_overlap()
    {
        Assert.Equal(NotificationRules.MenuMessages, NotificationRules.MenuCategory(NotificationRules.MessageType));
        Assert.Equal(NotificationRules.MenuFavorites, NotificationRules.MenuCategory(NotificationRules.PriceDropType));
        Assert.Equal(NotificationRules.MenuListings, NotificationRules.MenuCategory(NotificationRules.ListingType));
        Assert.Equal(NotificationRules.MenuOther, NotificationRules.MenuCategory("system"));
    }
}
