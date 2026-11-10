using FinalProject.MVC.Repositories.Implementations;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests;

[Collection("CampaignSql")]
public class CampaignPrecedenceTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb_Test;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public async Task Specific_and_all_parts_campaigns_can_both_be_active_with_product_precedence()
    {
        await using var db = CreateDb();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var catalog = new ShopCatalogService(new ApplicationRepository(db));
        await SilenceActiveCampaignsAsync(db);
        await ClearNamedAsync(db, "E2E Precedence ");

        var turbo = await db.ShopProducts.AsNoTracking()
            .SingleAsync(product => product.Name == "Turbocharger Assembly");
        var other = await db.ShopProducts.AsNoTracking()
            .Where(product => product.Id != turbo.Id && product.IsActive)
            .OrderBy(product => product.Id)
            .FirstAsync();

        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(14);

        var (specificOk, specificError) = await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E Precedence Turbo",
            ProductId = turbo.Id,
            DiscountPercent = 15,
            StartsAt = start,
            EndsAt = end,
            IsActive = true
        });
        Assert.True(specificOk, specificError);

        var (allOk, allError) = await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E Precedence All",
            AppliesToAllParts = true,
            DiscountPercent = 20,
            StartsAt = start,
            EndsAt = end,
            IsActive = true
        });
        Assert.True(allOk, allError);

        var specific = await db.ProductCampaigns.AsNoTracking()
            .SingleAsync(campaign => campaign.Name == "E2E Precedence Turbo");
        var allParts = await db.ProductCampaigns.AsNoTracking()
            .SingleAsync(campaign => campaign.Name == "E2E Precedence All");
        Assert.True(specific.IsActive);
        Assert.True(allParts.IsActive);

        // Reactivation of either side must remain allowed while the other stays active.
        Assert.True((await catalog.SetCampaignActiveAsync(allParts.Id, false)).Ok);
        Assert.True((await catalog.SetCampaignActiveAsync(allParts.Id, true)).Ok);
        Assert.True((await catalog.SetCampaignActiveAsync(specific.Id, false)).Ok);
        Assert.True((await catalog.SetCampaignActiveAsync(specific.Id, true)).Ok);

        var shop = await catalog.GetAllAsync();
        var turboShop = shop.Single(product => product.Id == turbo.Id);
        Assert.True(turboShop.HasOffer);
        Assert.Equal(15, turboShop.DiscountPercent);
        Assert.Equal("E2E Precedence Turbo", turboShop.OfferName);
        Assert.Equal(
            decimal.Round(turbo.Price * 0.85m, 2, MidpointRounding.AwayFromZero),
            turboShop.SalePrice);

        var otherShop = shop.Single(product => product.Id == other.Id);
        Assert.True(otherShop.HasOffer);
        Assert.Equal(20, otherShop.DiscountPercent);
        Assert.Equal("E2E Precedence All", otherShop.OfferName);
        Assert.Equal(
            decimal.Round(other.Price * 0.80m, 2, MidpointRounding.AwayFromZero),
            otherShop.SalePrice);

        Assert.All(
            shop.Where(product => product.Id != turbo.Id),
            product =>
            {
                Assert.True(product.HasOffer);
                Assert.Equal(20, product.DiscountPercent);
                Assert.Equal("E2E Precedence All", product.OfferName);
            });

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Two_all_parts_campaigns_still_conflict()
    {
        await using var db = CreateDb();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var catalog = new ShopCatalogService(new ApplicationRepository(db));
        await SilenceActiveCampaignsAsync(db);
        await ClearNamedAsync(db, "E2E Precedence ");

        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(5);
        Assert.True((await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E Precedence All A",
            AppliesToAllParts = true,
            DiscountPercent = 10,
            StartsAt = start,
            EndsAt = end,
            IsActive = true
        })).Ok);

        var second = await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E Precedence All B",
            AppliesToAllParts = true,
            DiscountPercent = 12,
            StartsAt = start,
            EndsAt = end,
            IsActive = true
        });
        Assert.False(second.Ok);
        Assert.Contains("already covers", second.Error, StringComparison.OrdinalIgnoreCase);

        await transaction.RollbackAsync();
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
        if (active.Count == 0)
        {
            return;
        }

        foreach (var campaign in active)
        {
            campaign.IsActive = false;
        }

        await db.SaveChangesAsync();
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
