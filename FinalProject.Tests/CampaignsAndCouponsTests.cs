using System.Reflection;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Controllers;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests;

[Collection("CampaignSql")]
public class CampaignsAndCouponsTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public void Admin_campaigns_and_coupons_require_Admin_or_SuperAdmin()
    {
        Assert.Equal("Admin,SuperAdmin", typeof(AdminCampaignsController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Admin,SuperAdmin", typeof(AdminCouponsController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.NotNull(typeof(AdminCouponsController).GetMethod(nameof(AdminCouponsController.Save))!
            .GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    [Theory]
    [InlineData(100, 10, 10)]
    [InlineData(99.99, 15, 15)]
    [InlineData(0, 10, 0)]
    public void Coupon_discount_is_calculated_server_side(decimal subtotal, decimal percent, decimal expected)
    {
        Assert.Equal(expected, CouponRules.CalculateDiscount(subtotal, percent));
    }

    [Fact]
    public void Coupon_rejects_expired_inactive_and_min_purchase()
    {
        var cart = new CartViewModel
        {
            Items = { new CartItem { ProductId = 1, ProductName = "A", Price = 20m, Quantity = 1 } }
        };
        var coupon = new Coupon
        {
            Code = "TEST10",
            DiscountPercent = 10,
            IsActive = false,
            AppliesToAllParts = true,
            MaxUsesPerUser = 1,
            MinimumPurchaseAmount = 0
        };
        Assert.Equal("This coupon is inactive.", CouponRules.ValidateForCart(coupon, cart, 0, 0, DateTime.UtcNow));

        coupon.IsActive = true;
        coupon.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        Assert.Equal("This coupon has expired.", CouponRules.ValidateForCart(coupon, cart, 0, 0, DateTime.UtcNow));

        coupon.ExpiresAt = null;
        coupon.MinimumPurchaseAmount = 50m;
        Assert.Contains("minimum purchase", CouponRules.ValidateForCart(coupon, cart, 0, 0, DateTime.UtcNow), StringComparison.OrdinalIgnoreCase);

        coupon.MinimumPurchaseAmount = 0;
        Assert.Equal("You have already used this coupon.", CouponRules.ValidateForCart(coupon, cart, 1, 0, DateTime.UtcNow));

        coupon.MaxTotalUses = 5;
        Assert.Contains("usage limit", CouponRules.ValidateForCart(coupon, cart, 0, 5, DateTime.UtcNow), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Product_scoped_coupon_only_uses_eligible_line_subtotal()
    {
        var cart = new CartViewModel
        {
            Items =
            {
                new CartItem { ProductId = 1, ProductName = "A", Price = 40m, Quantity = 1 },
                new CartItem { ProductId = 2, ProductName = "B", Price = 60m, Quantity = 1 }
            }
        };
        var coupon = new Coupon
        {
            Code = "PART2",
            DiscountPercent = 10,
            IsActive = true,
            AppliesToAllParts = false,
            ProductId = 2,
            MaxUsesPerUser = 1
        };
        Assert.Equal(60m, CouponRules.EligibleSubtotal(cart, coupon));
        Assert.Equal(6m, CouponRules.CalculateDiscount(60m, 10m));
        Assert.Null(CouponRules.ValidateForCart(coupon, cart, 0, 0, DateTime.UtcNow));
    }

    [Fact]
    public async Task Preview_apply_does_not_write_coupon_usage()
    {
        await using var db = CreateDb();
        var coupons = new CouponService(db);
        await coupons.EnsureSeedAsync();

        var userId = await db.Users.AsNoTracking().Select(user => user.Id).FirstAsync();
        var product = await db.ShopProducts.AsNoTracking().FirstAsync(item => item.IsActive && item.Price > 0);
        var cart = new CartViewModel
        {
            Items =
            {
                new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = 1
                }
            }
        };

        var usagesBefore = await db.CouponUsages.CountAsync();
        var (ok, message, discount, code) = await coupons.PreviewApplyAsync("cars100", userId, cart);
        Assert.True(ok, message);
        Assert.Equal("CARS100", code);
        Assert.Equal(CouponRules.CalculateDiscount(cart.Subtotal, 10m), discount);
        Assert.Equal(usagesBefore, await db.CouponUsages.CountAsync());
    }

    [Fact]
    public async Task Redeem_persists_usage_and_blocks_duplicate()
    {
        await using var db = CreateDb();
        var coupons = new CouponService(db);
        await coupons.EnsureSeedAsync();
        var userId = await db.Users.AsNoTracking().Select(user => user.Id).FirstAsync();
        var marker = "REDEEM-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        var (saved, saveError) = await coupons.SaveAsync(new Coupon
        {
            Code = marker,
            Name = "Redeem test",
            DiscountPercent = 5m,
            IsActive = true,
            AppliesToAllParts = true,
            MaxUsesPerUser = 1,
            MinimumPurchaseAmount = 0m
        });
        Assert.True(saved, saveError);

        try
        {
            var (ok, error) = await coupons.RedeemAsync(userId, marker, 5m);
            Assert.True(ok, error);
            Assert.Equal(1, await db.CouponUsages.CountAsync(item => item.UserId == userId && item.CouponCode == marker));

            var (again, againError) = await coupons.RedeemAsync(userId, marker, 5m);
            Assert.False(again);
            Assert.Contains("already used", againError, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM CouponUsages WHERE CouponCode = {marker};
                DELETE FROM Coupons WHERE Code = {marker};
                """);
        }
    }

    [Fact]
    public async Task Expired_and_inactive_coupons_fail_preview_without_mutating_usages()
    {
        await using var db = CreateDb();
        var coupons = new CouponService(db);
        var userId = await db.Users.AsNoTracking().Select(user => user.Id).FirstAsync();
        var product = await db.ShopProducts.AsNoTracking().FirstAsync(item => item.IsActive);
        var code = "EXP-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        var (saved, saveError) = await coupons.SaveAsync(new Coupon
        {
            Code = code,
            Name = "Expired",
            DiscountPercent = 10m,
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            AppliesToAllParts = true,
            MaxUsesPerUser = 1
        });
        Assert.True(saved, saveError);

        var cart = new CartViewModel
        {
            Items = { new CartItem { ProductId = product.Id, ProductName = "X", Price = 50m, Quantity = 1 } }
        };
        var usagesBefore = await db.CouponUsages.CountAsync();

        try
        {
            var (ok, message, _, _) = await coupons.PreviewApplyAsync(code, userId, cart);
            Assert.False(ok);
            Assert.Contains("expired", message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(usagesBefore, await db.CouponUsages.CountAsync());

            await coupons.SetActiveAsync((await coupons.GetByCodeAsync(code))!.Id, false);
            // Clear expiry so inactive is the failing reason
            var entity = await db.Coupons.SingleAsync(item => item.Code == code);
            entity.ExpiresAt = null;
            entity.IsActive = false;
            await db.SaveChangesAsync();

            var (inactiveOk, inactiveMessage, _, _) = await coupons.PreviewApplyAsync(code, userId, cart);
            Assert.False(inactiveOk);
            Assert.Contains("inactive", inactiveMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(usagesBefore, await db.CouponUsages.CountAsync());
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM CouponUsages WHERE CouponCode = {code};
                DELETE FROM Coupons WHERE Code = {code};
                """);
        }
    }

    [Fact]
    public async Task Campaign_save_rejects_inactive_product()
    {
        await using var db = CreateDb();
        var catalog = new ShopCatalogService(db);
        var product = await db.ShopProducts.FirstAsync(item => item.IsActive);
        product.IsActive = false;
        await db.SaveChangesAsync();

        try
        {
            var (ok, error) = await catalog.SaveCampaignAsync(new ProductCampaign
            {
                Name = "Inactive product campaign",
                ProductId = product.Id,
                AppliesToAllParts = false,
                DiscountPercent = 10m,
                StartsAt = DateTime.UtcNow.Date,
                EndsAt = DateTime.UtcNow.Date.AddDays(7),
                IsActive = true
            });
            Assert.False(ok);
            Assert.Contains("active", error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE ShopProducts SET IsActive = CAST(1 AS bit) WHERE Id = {product.Id};
                """);
        }
    }

    [Fact]
    public async Task Live_campaigns_load_from_database_for_shop_pricing()
    {
        await using var db = CreateDb();
        var catalog = new ShopCatalogService(db);
        var live = await catalog.GetLiveCampaignsAsync();
        Assert.All(live, campaign =>
        {
            Assert.True(campaign.IsActive);
            Assert.True(campaign.StartsAt <= DateTime.UtcNow);
            Assert.True(campaign.EndsAt > DateTime.UtcNow);
        });

        var products = await catalog.GetAllAsync();
        if (live.Count > 0)
        {
            Assert.Contains(products, product => product.HasOffer);
        }
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new ApplicationDbContext(options);
    }
}
