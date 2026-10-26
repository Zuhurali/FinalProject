using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Services;

public sealed class CouponService : ICouponService
{
    private readonly ApplicationDbContext _db;

    public CouponService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Coupon>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Coupons.AsNoTracking()
            .Include(coupon => coupon.Product)
            .OrderByDescending(coupon => coupon.IsActive)
            .ThenBy(coupon => coupon.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<Coupon?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Coupons.AsNoTracking()
            .Include(coupon => coupon.Product)
            .FirstOrDefaultAsync(coupon => coupon.Id == id, cancellationToken);
    }

    public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = CouponRules.NormalizeCode(code);
        if (normalized.Length == 0)
        {
            return null;
        }

        return await _db.Coupons.AsNoTracking()
            .FirstOrDefaultAsync(coupon => coupon.Code == normalized, cancellationToken);
    }

    public async Task<(bool Ok, string? Error)> SaveAsync(
        Coupon coupon,
        CancellationToken cancellationToken = default)
    {
        var definitionError = CouponRules.ValidateDefinition(coupon);
        if (definitionError != null)
        {
            return (false, definitionError);
        }

        if (!coupon.AppliesToAllParts)
        {
            var productOk = await _db.ShopProducts.AsNoTracking()
                .AnyAsync(product => product.Id == coupon.ProductId!.Value && product.IsActive, cancellationToken);
            if (!productOk)
            {
                return (false, "Choose an active spare part for this coupon.");
            }
        }

        var duplicate = await _db.Coupons.AsNoTracking()
            .AnyAsync(
                item => item.Code == coupon.Code && item.Id != coupon.Id,
                cancellationToken);
        if (duplicate)
        {
            return (false, $"Coupon code {coupon.Code} already exists.");
        }

        if (coupon.Id <= 0)
        {
            coupon.Id = 0;
            coupon.CreatedAt = DateTime.UtcNow;
            coupon.Product = null;
            _db.Coupons.Add(coupon);
        }
        else
        {
            var existing = await _db.Coupons.FirstOrDefaultAsync(item => item.Id == coupon.Id, cancellationToken);
            if (existing == null)
            {
                return (false, "Coupon not found.");
            }

            existing.Code = coupon.Code;
            existing.Name = coupon.Name;
            existing.DiscountPercent = coupon.DiscountPercent;
            existing.IsActive = coupon.IsActive;
            existing.ExpiresAt = coupon.ExpiresAt;
            existing.MinimumPurchaseAmount = coupon.MinimumPurchaseAmount;
            existing.MaxUsesPerUser = coupon.MaxUsesPerUser;
            existing.MaxTotalUses = coupon.MaxTotalUses;
            existing.AppliesToAllParts = coupon.AppliesToAllParts;
            existing.ProductId = coupon.ProductId;
            existing.Product = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<bool> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var coupon = await _db.Coupons.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (coupon == null)
        {
            return false;
        }

        coupon.IsActive = isActive;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(bool Ok, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var coupon = await _db.Coupons.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (coupon == null)
        {
            return (false, "Coupon not found.");
        }

        var usages = await _db.CouponUsages.CountAsync(
            item => item.CouponCode == coupon.Code,
            cancellationToken);
        if (usages > 0)
        {
            coupon.IsActive = false;
            await _db.SaveChangesAsync(cancellationToken);
            return (false, "This coupon already has redemptions, so it was deactivated instead of deleted.");
        }

        _db.Coupons.Remove(coupon);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Ok, string Message, decimal DiscountAmount, string? NormalizedCode)> PreviewApplyAsync(
        string? code,
        string userId,
        CartViewModel cart,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return (false, "Please sign in to use a coupon.", 0m, null);
        }

        var normalized = CouponRules.NormalizeCode(code);
        var coupon = await GetByCodeAsync(normalized, cancellationToken);
        var userRedemptions = await _db.CouponUsages.CountAsync(
            item => item.UserId == userId && item.CouponCode == normalized,
            cancellationToken);
        var totalRedemptions = await _db.CouponUsages.CountAsync(
            item => item.CouponCode == normalized,
            cancellationToken);

        var error = CouponRules.ValidateForCart(
            coupon,
            cart,
            userRedemptions,
            totalRedemptions,
            DateTime.UtcNow);
        if (error != null || coupon == null)
        {
            return (false, error ?? "Invalid coupon code.", 0m, null);
        }

        var eligible = CouponRules.EligibleSubtotal(cart, coupon);
        var discount = CouponRules.CalculateDiscount(eligible, coupon.DiscountPercent);
        if (discount <= 0)
        {
            return (false, "This coupon does not produce a discount for the current cart.", 0m, null);
        }

        if (discount > cart.Subtotal)
        {
            discount = cart.Subtotal;
        }

        return (
            true,
            $"{coupon.Code} applied ({coupon.DiscountPercent:0.#}% off). The discount is reserved for checkout/order completion and is not permanently used yet.",
            discount,
            coupon.Code);
    }

    public async Task<(bool Ok, string? Error)> RedeemAsync(
        string userId,
        string? code,
        decimal discountAmount,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return (false, "User is required to redeem a coupon.");
        }

        var normalized = CouponRules.NormalizeCode(code);
        if (normalized.Length == 0)
        {
            return (true, null);
        }

        var coupon = await GetByCodeAsync(normalized, cancellationToken);
        if (coupon == null)
        {
            return (false, "Coupon not found.");
        }

        var userRedemptions = await _db.CouponUsages.CountAsync(
            item => item.UserId == userId && item.CouponCode == normalized,
            cancellationToken);
        if (userRedemptions >= coupon.MaxUsesPerUser)
        {
            return (false, "You have already used this coupon.");
        }

        if (coupon.MaxTotalUses.HasValue)
        {
            var total = await _db.CouponUsages.CountAsync(
                item => item.CouponCode == normalized,
                cancellationToken);
            if (total >= coupon.MaxTotalUses.Value)
            {
                return (false, "This coupon has reached its usage limit.");
            }
        }

        _db.CouponUsages.Add(new CouponUsage
        {
            UserId = userId,
            CouponCode = normalized,
            UsedAt = DateTime.UtcNow,
            DiscountAmount = Math.Max(0m, decimal.Round(discountAmount, 2, MidpointRounding.AwayFromZero))
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return (true, null);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return (false, "You have already used this coupon.");
        }
    }

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.Coupons.AnyAsync(coupon => coupon.Code == "CARS100", cancellationToken))
        {
            return;
        }

        _db.Coupons.Add(new Coupon
        {
            Code = "CARS100",
            Name = "Showroom 10% spare-parts courtesy",
            DiscountPercent = 10m,
            IsActive = true,
            ExpiresAt = null,
            MinimumPurchaseAmount = 0m,
            MaxUsesPerUser = 1,
            MaxTotalUses = null,
            AppliesToAllParts = true,
            ProductId = null,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
