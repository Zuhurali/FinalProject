using FinalProject.MVC.Models;

namespace FinalProject.MVC.Commerce;

public static class CouponRules
{
    public static string NormalizeCode(string? code) =>
        (code ?? string.Empty).Trim().ToUpperInvariant();

    public static string? ValidateDefinition(Coupon coupon)
    {
        coupon.Code = NormalizeCode(coupon.Code);
        coupon.Name = (coupon.Name ?? string.Empty).Trim();

        if (coupon.Code.Length is < 3 or > 50)
        {
            return "Enter a coupon code between 3 and 50 characters.";
        }

        if (coupon.Code.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '-' or '_')))
        {
            return "Coupon codes may only contain letters, numbers, hyphens, and underscores.";
        }

        if (coupon.Name.Length is < 2 or > 200)
        {
            return "Enter a short display name for the coupon.";
        }

        if (coupon.DiscountPercent is <= 0 or > 100)
        {
            return "Discount percent must be greater than 0 and at most 100.";
        }

        if (coupon.MinimumPurchaseAmount < 0)
        {
            return "Minimum purchase cannot be negative.";
        }

        if (coupon.MaxUsesPerUser < 1)
        {
            return "Max uses per user must be at least 1.";
        }

        if (coupon.MaxTotalUses is <= 0)
        {
            return "Max total uses must be greater than zero when set.";
        }

        if (coupon.ExpiresAt.HasValue
            && coupon.ExpiresAt.Value.Kind == DateTimeKind.Unspecified)
        {
            coupon.ExpiresAt = DateTime.SpecifyKind(coupon.ExpiresAt.Value, DateTimeKind.Utc);
        }

        if (!coupon.AppliesToAllParts && coupon.ProductId is null or <= 0)
        {
            return "Choose a spare part, or make the coupon apply to all parts.";
        }

        if (coupon.AppliesToAllParts)
        {
            coupon.ProductId = null;
        }

        return null;
    }

    public static decimal EligibleSubtotal(CartViewModel cart, Coupon coupon)
    {
        if (coupon.AppliesToAllParts || coupon.ProductId is null)
        {
            return cart.Subtotal;
        }

        return cart.Items
            .Where(item => item.ProductId == coupon.ProductId.Value)
            .Sum(item => item.Subtotal);
    }

    public static decimal CalculateDiscount(decimal eligibleSubtotal, decimal discountPercent)
    {
        if (eligibleSubtotal <= 0 || discountPercent <= 0)
        {
            return 0m;
        }

        var amount = eligibleSubtotal * (discountPercent / 100m);
        return decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    public static string? ValidateForCart(
        Coupon? coupon,
        CartViewModel cart,
        int userRedemptions,
        int totalRedemptions,
        DateTime utcNow)
    {
        if (coupon == null)
        {
            return "Invalid coupon code.";
        }

        if (!coupon.IsActive)
        {
            return "This coupon is inactive.";
        }

        if (coupon.ExpiresAt.HasValue && utcNow >= coupon.ExpiresAt.Value)
        {
            return "This coupon has expired.";
        }

        if (cart.IsEmpty)
        {
            return "Add items to your cart before applying a coupon.";
        }

        var eligible = EligibleSubtotal(cart, coupon);
        if (eligible <= 0)
        {
            return coupon.AppliesToAllParts
                ? "This coupon does not apply to the current cart."
                : "This coupon does not apply to any items in your cart.";
        }

        if (eligible < coupon.MinimumPurchaseAmount)
        {
            return $"A minimum purchase of {coupon.MinimumPurchaseAmount:C} (eligible items) is required.";
        }

        if (userRedemptions >= coupon.MaxUsesPerUser)
        {
            return "You have already used this coupon.";
        }

        if (coupon.MaxTotalUses.HasValue && totalRedemptions >= coupon.MaxTotalUses.Value)
        {
            return "This coupon has reached its usage limit.";
        }

        return null;
    }
}
