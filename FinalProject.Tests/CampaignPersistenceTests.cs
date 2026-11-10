using FinalProject.MVC.Repositories.Implementations;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests;

[CollectionDefinition("CampaignSql")]
public sealed class CampaignSqlCollection : ICollectionFixture<object>
{
}

[Collection("CampaignSql")]
public class CampaignPersistenceTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb_Test;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Campaigns_save_to_sql_and_only_live_spare_part_offers_change_shop_prices()
    {
        await using var db = CreateDb();
        var catalog = new ShopCatalogService(new ApplicationRepository(db));

        var turbo = await db.ShopProducts.AsNoTracking().SingleAsync(product => product.Name == "Turbocharger Assembly");
        var originalPrice = turbo.Price;
        var originalQty = turbo.Quantity;
        Assert.True(originalPrice > 0);

        var paused = await db.ProductCampaigns
            .Where(campaign => campaign.IsActive && !campaign.Name.StartsWith("E2E "))
            .ToListAsync();
        foreach (var campaign in paused)
        {
            campaign.IsActive = false;
        }

        if (paused.Count > 0)
        {
            await db.SaveChangesAsync();
        }

        await ClearNamedCampaignsAsync(db, "E2E ");

        var start = DateTime.UtcNow.Date;
        var expectedSale = ShowroomRules.DiscountedPrice(originalPrice, null, 15);
        var (ok, error) = await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E Turbo 15",
            ProductId = turbo.Id,
            DiscountPercent = 15,
            StartsAt = start,
            EndsAt = start.AddDays(14),
            IsActive = true
        });
        Assert.True(ok, error);

        var saved = await db.ProductCampaigns.AsNoTracking().SingleAsync(campaign => campaign.Name == "E2E Turbo 15");
        Assert.Equal(turbo.Id, saved.ProductId);
        Assert.False(saved.AppliesToAllParts);
        Assert.Equal(15m, saved.DiscountPercent);

        var shop = await catalog.GetAllAsync();
        var turboShop = shop.Single(product => product.Id == turbo.Id);
        Assert.True(turboShop.HasOffer);
        Assert.Equal(expectedSale, turboShop.SalePrice);
        Assert.Equal(15, turboShop.DiscountPercent);
        Assert.False(string.IsNullOrWhiteSpace(turboShop.OfferEndLabel));
        Assert.All(shop.Where(product => product.Id != turbo.Id), product => Assert.False(product.HasOffer));

        var after = await db.ShopProducts.AsNoTracking().SingleAsync(product => product.Id == turbo.Id);
        Assert.Equal(originalPrice, after.Price);
        Assert.Equal(originalQty, after.Quantity);

        var (saleOk, saleError) = await catalog.RecordSaleAsync(turbo.Id, 1, "e2e-campaign-test", "E2E live campaign sale");
        Assert.True(saleOk, saleError);
        var sale = await db.InPersonSales.AsNoTracking()
            .Where(item => item.Note == "E2E live campaign sale")
            .OrderByDescending(item => item.Id)
            .FirstAsync();
        Assert.Equal(originalPrice, sale.ListUnitPrice);
        Assert.Equal(expectedSale, sale.UnitPrice);
        Assert.Equal(saved.Id, sale.CampaignId);
        Assert.Equal(15m, sale.CampaignDiscountPercent);
        Assert.False(sale.IsDemo);
        var sold = await db.ShopProducts.FirstAsync(product => product.Id == turbo.Id);
        sold.Quantity = originalQty;
        sold.InStock = originalQty > 0;
        await db.SaveChangesAsync();
        db.InPersonSales.Remove(await db.InPersonSales.SingleAsync(item => item.Id == sale.Id));
        await db.SaveChangesAsync();

        var overlap = await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E All parts overlap",
            AppliesToAllParts = true,
            DiscountPercent = 10,
            StartsAt = start,
            EndsAt = start.AddDays(7),
            IsActive = true
        });
        Assert.True(overlap.Ok, overlap.Error);
        shop = await catalog.GetAllAsync();
        turboShop = shop.Single(product => product.Id == turbo.Id);
        Assert.Equal(15, turboShop.DiscountPercent);
        Assert.Equal(expectedSale, turboShop.SalePrice);
        Assert.Equal("E2E Turbo 15", turboShop.OfferName);
        var otherWithAllParts = shop.First(product => product.Id != turbo.Id);
        Assert.True(otherWithAllParts.HasOffer);
        Assert.Equal(10, otherWithAllParts.DiscountPercent);
        Assert.Equal("E2E All parts overlap", otherWithAllParts.OfferName);

        var invalidDates = await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E inverted dates",
            ProductId = turbo.Id,
            DiscountPercent = 15,
            StartsAt = start.AddDays(5),
            EndsAt = start,
            IsActive = false
        });
        Assert.False(invalidDates.Ok);

        var invalidPercent = await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E bad percent",
            ProductId = turbo.Id,
            DiscountPercent = 0,
            StartsAt = start,
            EndsAt = start.AddDays(2),
            IsActive = false
        });
        Assert.False(invalidPercent.Ok);

        var expired = await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E expired",
            ProductId = turbo.Id,
            DiscountPercent = 40,
            StartsAt = start.AddDays(-20),
            EndsAt = start.AddDays(-10),
            IsActive = true
        });
        Assert.True(expired.Ok, expired.Error);

        var inactive = await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E inactive",
            ProductId = turbo.Id,
            DiscountPercent = 50,
            StartsAt = start,
            EndsAt = start.AddDays(3),
            IsActive = false
        });
        Assert.True(inactive.Ok, inactive.Error);

        shop = await catalog.GetAllAsync();
        turboShop = shop.Single(product => product.Id == turbo.Id);
        Assert.Equal(15, turboShop.DiscountPercent);
        Assert.Equal(expectedSale, turboShop.SalePrice);

        await ClearNamedCampaignsAsync(db, "E2E ");

        var (allOk, allError) = await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E All parts 10",
            AppliesToAllParts = true,
            DiscountPercent = 10,
            StartsAt = start,
            EndsAt = start.AddDays(5),
            IsActive = true
        });
        Assert.True(allOk, allError);
        shop = await catalog.GetAllAsync();
        Assert.NotEmpty(shop);
        Assert.All(shop, product =>
        {
            Assert.True(product.HasOffer);
            Assert.Equal(10, product.DiscountPercent);
            Assert.Equal(ShowroomRules.DiscountedPrice(product.Price, null, 10), product.SalePrice);
        });

        await ClearNamedCampaignsAsync(db, "E2E All parts 10");

        var (restoreOk, restoreError) = await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E Turbo 15",
            ProductId = turbo.Id,
            DiscountPercent = 15,
            StartsAt = start,
            EndsAt = start.AddDays(14),
            IsActive = true
        });
        Assert.True(restoreOk, restoreError);

        foreach (var campaign in paused)
        {
            var row = await db.ProductCampaigns.FirstOrDefaultAsync(item => item.Id == campaign.Id);
            if (row != null)
            {
                row.IsActive = true;
            }
        }

        if (paused.Count > 0)
        {
            await db.SaveChangesAsync();
        }
    }

    private static async Task ClearNamedCampaignsAsync(ApplicationDbContext db, string namePrefix)
    {
        var matches = await db.ProductCampaigns
            .Where(campaign => campaign.Name.StartsWith(namePrefix))
            .ToListAsync();
        if (matches.Count == 0)
        {
            return;
        }

        db.ProductCampaigns.RemoveRange(matches);
        await db.SaveChangesAsync();
    }
}
