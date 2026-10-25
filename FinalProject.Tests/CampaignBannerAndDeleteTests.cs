using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests;

[Collection("CampaignSql")]
public class CampaignBannerAndDeleteTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public async Task Live_banners_include_only_active_in_window_campaigns_and_hide_when_none()
    {
        await using var db = CreateDb();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var catalog = new ShopCatalogService(db);
        await SilenceActiveCampaignsAsync(db);
        await ClearNamedAsync(db, "E2E Banner ");

        var turbo = await db.ShopProducts.AsNoTracking()
            .SingleAsync(product => product.Name == "Turbocharger Assembly");
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(10);

        Assert.True((await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E Banner Turbo",
            ProductId = turbo.Id,
            DiscountPercent = 15,
            StartsAt = start,
            EndsAt = end,
            IsActive = true
        })).Ok);
        Assert.True((await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E Banner All",
            AppliesToAllParts = true,
            DiscountPercent = 10,
            StartsAt = start,
            EndsAt = end,
            IsActive = true
        })).Ok);
        Assert.True((await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E Banner Expired",
            ProductId = turbo.Id,
            DiscountPercent = 40,
            StartsAt = start.AddDays(-20),
            EndsAt = start.AddDays(-5),
            IsActive = true
        })).Ok);
        Assert.True((await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E Banner Inactive",
            AppliesToAllParts = true,
            DiscountPercent = 25,
            StartsAt = start,
            EndsAt = end,
            IsActive = false
        })).Ok);

        var live = await catalog.GetLiveCampaignsAsync();
        Assert.Equal(2, live.Count);
        Assert.Contains(live, campaign => campaign.Name == "E2E Banner Turbo");
        Assert.Contains(live, campaign => campaign.Name == "E2E Banner All");
        Assert.DoesNotContain(live, campaign => campaign.Name == "E2E Banner Expired");
        Assert.DoesNotContain(live, campaign => campaign.Name == "E2E Banner Inactive");

        var turboBanner = live.Single(campaign => campaign.Name == "E2E Banner Turbo");
        Assert.Equal(
            turbo.ImagePath,
            ShowroomRules.CampaignBannerImagePath(turboBanner.Product?.ImagePath, turboBanner.AppliesToAllParts));
        Assert.Equal(
            ShowroomRules.AllPartsCampaignImagePath,
            ShowroomRules.CampaignBannerImagePath(null, true));

        var shop = await catalog.GetAllAsync();
        var turboShop = shop.Single(product => product.Id == turbo.Id);
        Assert.Equal(turboBanner.Id, turboShop.OfferCampaignId);
        Assert.Equal(15, turboShop.DiscountPercent);
        Assert.All(
            shop.Where(product => product.Id != turbo.Id),
            product =>
            {
                Assert.Equal(live.Single(campaign => campaign.Name == "E2E Banner All").Id, product.OfferCampaignId);
                Assert.Equal(10, product.DiscountPercent);
            });

        var inactive = await db.ProductCampaigns.SingleAsync(campaign => campaign.Name == "E2E Banner Turbo");
        inactive.IsActive = false;
        await db.SaveChangesAsync();
        live = await catalog.GetLiveCampaignsAsync();
        Assert.DoesNotContain(live, campaign => campaign.Name == "E2E Banner Turbo");
        Assert.Single(live);

        foreach (var campaign in await db.ProductCampaigns.Where(item => item.Name.StartsWith("E2E Banner ")).ToListAsync())
        {
            campaign.IsActive = false;
        }

        await db.SaveChangesAsync();
        Assert.Empty(await catalog.GetLiveCampaignsAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Delete_removes_one_campaign_and_leaves_others_and_products_untouched()
    {
        await using var db = CreateDb();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var catalog = new ShopCatalogService(db);
        await SilenceActiveCampaignsAsync(db);
        await ClearNamedAsync(db, "E2E Delete ");

        var turbo = await db.ShopProducts.AsNoTracking()
            .SingleAsync(product => product.Name == "Turbocharger Assembly");
        var originalPrice = turbo.Price;
        var originalQty = turbo.Quantity;
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(8);

        Assert.True((await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E Delete Keep",
            AppliesToAllParts = true,
            DiscountPercent = 12,
            StartsAt = start,
            EndsAt = end,
            IsActive = true
        })).Ok);
        Assert.True((await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E Delete Target",
            ProductId = turbo.Id,
            DiscountPercent = 18,
            StartsAt = start,
            EndsAt = end,
            IsActive = true
        })).Ok);

        var keep = await db.ProductCampaigns.AsNoTracking()
            .SingleAsync(campaign => campaign.Name == "E2E Delete Keep");
        var target = await db.ProductCampaigns.AsNoTracking()
            .SingleAsync(campaign => campaign.Name == "E2E Delete Target");

        var (deleted, deleteError) = await catalog.DeleteCampaignAsync(target.Id);
        Assert.True(deleted, deleteError);
        Assert.Null(await db.ProductCampaigns.AsNoTracking()
            .SingleOrDefaultAsync(campaign => campaign.Id == target.Id));
        Assert.NotNull(await db.ProductCampaigns.AsNoTracking()
            .SingleOrDefaultAsync(campaign => campaign.Id == keep.Id));

        var product = await db.ShopProducts.AsNoTracking().SingleAsync(item => item.Id == turbo.Id);
        Assert.Equal(originalPrice, product.Price);
        Assert.Equal(originalQty, product.Quantity);

        var live = await catalog.GetLiveCampaignsAsync();
        Assert.DoesNotContain(live, campaign => campaign.Id == target.Id);
        Assert.Contains(live, campaign => campaign.Id == keep.Id);

        var shop = await catalog.GetAllAsync();
        Assert.All(shop, item => Assert.Equal(keep.Id, item.OfferCampaignId));

        var missing = await catalog.DeleteCampaignAsync(target.Id);
        Assert.False(missing.Ok);
        Assert.Equal("Campaign not found.", missing.Error);

        await transaction.RollbackAsync();
    }

    [Fact]
    public void Campaign_banner_views_use_live_campaign_data_not_hardcoded_names()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var home = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Home", "Index.cshtml"));
        var shop = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shop", "Index.cshtml"));
        var admin = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminCampaigns", "Index.cshtml"));

        Assert.Contains("LiveCampaigns", home, StringComparison.Ordinal);
        Assert.Contains("campaign = liveCampaign.Id", home, StringComparison.Ordinal);
        Assert.DoesNotContain("E2E Turbo 15", home, StringComparison.Ordinal);
        var homeController = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "HomeController.cs"));
        Assert.Contains("CampaignBannerSelection.ForHome", homeController, StringComparison.Ordinal);
        Assert.Contains("LiveCampaigns", shop, StringComparison.Ordinal);
        Assert.Contains("campaign = liveCampaign.Id", shop, StringComparison.Ordinal);
        Assert.Contains("asp-action=\"Delete\"", admin, StringComparison.Ordinal);
        Assert.Contains("method=\"post\"", admin, StringComparison.Ordinal);
        Assert.Contains("AntiForgeryToken", admin, StringComparison.Ordinal);
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task SilenceActiveCampaignsAsync(ApplicationDbContext db)
    {
        var active = await db.ProductCampaigns.Where(campaign => campaign.IsActive).ToListAsync();
        foreach (var campaign in active)
        {
            campaign.IsActive = false;
        }

        if (active.Count > 0)
        {
            await db.SaveChangesAsync();
        }
    }

    private static async Task ClearNamedAsync(ApplicationDbContext db, string prefix)
    {
        var matches = await db.ProductCampaigns
            .Where(campaign => campaign.Name.StartsWith(prefix))
            .ToListAsync();
        if (matches.Count == 0)
        {
            return;
        }

        db.ProductCampaigns.RemoveRange(matches);
        await db.SaveChangesAsync();
    }
}
