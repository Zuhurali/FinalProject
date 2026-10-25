using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests;

public class CampaignHomeBannerSelectionTests
{
    [Fact]
    public void Home_banners_include_all_parts_cap_at_three_and_order_by_newest_start_then_id()
    {
        var older = new ProductCampaign
        {
            Id = 10,
            Name = "Older single",
            AppliesToAllParts = false,
            ProductId = 1,
            StartsAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            EndsAt = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            DiscountPercent = 10
        };
        var newerLowId = new ProductCampaign
        {
            Id = 20,
            Name = "Newer A",
            AppliesToAllParts = false,
            ProductId = 2,
            StartsAt = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc),
            EndsAt = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            DiscountPercent = 15
        };
        var newerHighId = new ProductCampaign
        {
            Id = 30,
            Name = "Newer B",
            AppliesToAllParts = false,
            ProductId = 3,
            StartsAt = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc),
            EndsAt = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            DiscountPercent = 12
        };
        var newest = new ProductCampaign
        {
            Id = 5,
            Name = "Newest",
            AppliesToAllParts = false,
            ProductId = 4,
            StartsAt = new DateTime(2026, 10, 22, 0, 0, 0, DateTimeKind.Utc),
            EndsAt = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            DiscountPercent = 18
        };
        var allParts = new ProductCampaign
        {
            Id = 99,
            Name = "All spare parts",
            AppliesToAllParts = true,
            StartsAt = new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc),
            EndsAt = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            DiscountPercent = 20
        };
        var fourthNewest = new ProductCampaign
        {
            Id = 40,
            Name = "Fourth",
            AppliesToAllParts = false,
            ProductId = 5,
            StartsAt = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc),
            EndsAt = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            DiscountPercent = 8
        };

        var selected = CampaignBannerSelection.ForHome(
        [
            older,
            allParts,
            newerHighId,
            fourthNewest,
            newerLowId,
            newest
        ]);

        Assert.Equal(3, selected.Count);
        Assert.Contains(selected, campaign => campaign.AppliesToAllParts && campaign.Id == allParts.Id);
        Assert.Equal(new[] { allParts.Id, newest.Id, newerLowId.Id }, selected.Select(campaign => campaign.Id));
        Assert.DoesNotContain(selected, campaign => campaign.Id == older.Id || campaign.Id == fourthNewest.Id || campaign.Id == newerHighId.Id);
    }

    [Fact]
    public void Home_banners_show_only_available_eligible_campaigns_when_fewer_than_three()
    {
        var one = new ProductCampaign
        {
            Id = 1,
            Name = "Only",
            AppliesToAllParts = false,
            ProductId = 7,
            StartsAt = DateTime.UtcNow.Date,
            EndsAt = DateTime.UtcNow.Date.AddDays(5),
            IsActive = true,
            DiscountPercent = 10
        };
        var allParts = new ProductCampaign
        {
            Id = 2,
            Name = "All",
            AppliesToAllParts = true,
            StartsAt = DateTime.UtcNow.Date,
            EndsAt = DateTime.UtcNow.Date.AddDays(5),
            IsActive = true,
            DiscountPercent = 20
        };

        var selected = CampaignBannerSelection.ForHome([one, allParts]);
        Assert.Equal(2, selected.Count);
        Assert.Equal(new[] { one.Id, allParts.Id }, selected.Select(campaign => campaign.Id));
        Assert.Single(CampaignBannerSelection.ForHome([allParts]));
        Assert.Equal(allParts.Id, CampaignBannerSelection.ForHome([allParts])[0].Id);
    }
}

[Collection("CampaignSql")]
public class CampaignHomeBannerShopTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public async Task All_parts_campaign_appears_on_home_and_shop_filter_shows_its_eligible_products()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var catalog = new ShopCatalogService(db);

        var active = await db.ProductCampaigns.Where(campaign => campaign.IsActive).ToListAsync();
        foreach (var campaign in active)
        {
            campaign.IsActive = false;
        }

        if (active.Count > 0)
        {
            await db.SaveChangesAsync();
        }

        var matches = await db.ProductCampaigns
            .Where(campaign => campaign.Name.StartsWith("E2E HomeBanner "))
            .ToListAsync();
        if (matches.Count > 0)
        {
            db.ProductCampaigns.RemoveRange(matches);
            await db.SaveChangesAsync();
        }

        var products = await db.ShopProducts.AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.Id)
            .Take(5)
            .ToListAsync();
        Assert.True(products.Count >= 5);

        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(12);
        // Four live singles with staggered past starts; all-parts newest so it ranks into home top 3.
        for (var index = 0; index < 4; index++)
        {
            Assert.True((await catalog.SaveCampaignAsync(new ProductCampaign
            {
                Name = $"E2E HomeBanner Single {index + 1}",
                ProductId = products[index].Id,
                DiscountPercent = 11 + index,
                StartsAt = start.AddDays(index - 3),
                EndsAt = end,
                IsActive = true
            })).Ok);
        }

        Assert.True((await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E HomeBanner All",
            AppliesToAllParts = true,
            DiscountPercent = 7,
            StartsAt = start,
            EndsAt = end,
            IsActive = true
        })).Ok);

        var live = await catalog.GetLiveCampaignsAsync();
        Assert.Contains(live, campaign => campaign.AppliesToAllParts && campaign.Name == "E2E HomeBanner All");
        Assert.Equal(5, live.Count);

        var home = CampaignBannerSelection.ForHome(live);
        Assert.Equal(3, home.Count);
        Assert.Contains(home, campaign => campaign.AppliesToAllParts && campaign.Name == "E2E HomeBanner All");
        Assert.Equal(
            live.OrderByDescending(campaign => campaign.StartsAt)
                .ThenBy(campaign => campaign.Id)
                .Take(3)
                .Select(campaign => campaign.Id),
            home.Select(campaign => campaign.Id));

        var shop = await catalog.GetAllAsync();
        var allPartsId = live.Single(campaign => campaign.Name == "E2E HomeBanner All").Id;
        var specificProductIds = live
            .Where(campaign => !campaign.AppliesToAllParts)
            .Select(campaign => campaign.ProductId!.Value)
            .ToHashSet();

        var filteredByAllPartsBanner = shop
            .Where(product => product.HasOffer && product.OfferCampaignId == allPartsId)
            .ToList();
        Assert.NotEmpty(filteredByAllPartsBanner);
        Assert.All(filteredByAllPartsBanner, product =>
        {
            Assert.Equal(7, product.DiscountPercent);
            Assert.DoesNotContain(product.Id, specificProductIds);
        });
        Assert.Contains(products[4].Id, filteredByAllPartsBanner.Select(product => product.Id));

        await transaction.RollbackAsync();
    }
}
