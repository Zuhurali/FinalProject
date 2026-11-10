using FinalProject.MVC.Repositories.Implementations;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests;

[Collection("CampaignSql")]
public class CampaignActivationTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb_Test;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public async Task Deactivate_then_reactivate_persists_is_active_without_changing_dates()
    {
        await using var db = CreateDb();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var catalog = new ShopCatalogService(new ApplicationRepository(db));
        await SilenceActiveCampaignsAsync(db);
        var turboId = await db.ShopProducts.AsNoTracking()
            .Where(product => product.Name == "Turbocharger Assembly")
            .Select(product => product.Id)
            .SingleAsync();

        await ClearNamedAsync(db, "E2E Activation ");
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(10);
        var (created, createError) = await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E Activation Live",
            ProductId = turboId,
            DiscountPercent = 12,
            StartsAt = start,
            EndsAt = end,
            IsActive = true
        });
        Assert.True(created, createError);

        var campaign = await db.ProductCampaigns.AsNoTracking()
            .SingleAsync(item => item.Name == "E2E Activation Live");
        Assert.True(campaign.IsActive);
        var storedStart = campaign.StartsAt;
        var storedEnd = campaign.EndsAt;

        var (deactivated, deactivateError) = await catalog.SetCampaignActiveAsync(campaign.Id, false);
        Assert.True(deactivated, deactivateError);
        var afterOff = await db.ProductCampaigns.AsNoTracking().SingleAsync(item => item.Id == campaign.Id);
        Assert.False(afterOff.IsActive);
        Assert.Equal(storedStart, afterOff.StartsAt);
        Assert.Equal(storedEnd, afterOff.EndsAt);
        Assert.Equal(12m, afterOff.DiscountPercent);

        var (activated, activateError) = await catalog.SetCampaignActiveAsync(campaign.Id, true);
        Assert.True(activated, activateError);
        var afterOn = await db.ProductCampaigns.AsNoTracking().SingleAsync(item => item.Id == campaign.Id);
        Assert.True(afterOn.IsActive);
        Assert.Equal(storedStart, afterOn.StartsAt);
        Assert.Equal(storedEnd, afterOn.EndsAt);
        Assert.Equal(12m, afterOn.DiscountPercent);
        Assert.Equal("Live", ShowroomRules.CampaignPublicStatus(afterOn.IsActive, afterOn.StartsAt, afterOn.EndsAt, DateTime.UtcNow));

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Reactivate_outside_date_window_persists_active_but_is_not_live()
    {
        await using var db = CreateDb();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var catalog = new ShopCatalogService(new ApplicationRepository(db));
        await SilenceActiveCampaignsAsync(db);
        var turboId = await db.ShopProducts.AsNoTracking()
            .Where(product => product.Name == "Turbocharger Assembly")
            .Select(product => product.Id)
            .SingleAsync();

        await ClearNamedAsync(db, "E2E Activation ");
        var start = DateTime.UtcNow.Date.AddDays(-20);
        var end = DateTime.UtcNow.Date.AddDays(-5);
        var (created, createError) = await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E Activation Expired",
            ProductId = turboId,
            DiscountPercent = 18,
            StartsAt = start,
            EndsAt = end,
            IsActive = false
        });
        Assert.True(created, createError);

        var campaign = await db.ProductCampaigns.AsNoTracking()
            .SingleAsync(item => item.Name == "E2E Activation Expired");
        var storedStart = campaign.StartsAt;
        var storedEnd = campaign.EndsAt;
        var (activated, activateError) = await catalog.SetCampaignActiveAsync(campaign.Id, true);
        Assert.True(activated, activateError);

        var stored = await db.ProductCampaigns.AsNoTracking().SingleAsync(item => item.Id == campaign.Id);
        Assert.True(stored.IsActive);
        Assert.Equal(storedStart, stored.StartsAt);
        Assert.Equal(storedEnd, stored.EndsAt);
        Assert.Equal(
            "Expired",
            ShowroomRules.CampaignPublicStatus(stored.IsActive, stored.StartsAt, stored.EndsAt, DateTime.UtcNow));

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Reactivate_blocked_when_another_active_campaign_overlaps()
    {
        await using var db = CreateDb();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var catalog = new ShopCatalogService(new ApplicationRepository(db));
        await SilenceActiveCampaignsAsync(db);
        var turboId = await db.ShopProducts.AsNoTracking()
            .Where(product => product.Name == "Turbocharger Assembly")
            .Select(product => product.Id)
            .SingleAsync();

        await ClearNamedAsync(db, "E2E Activation ");
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(7);

        Assert.True((await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E Activation First",
            ProductId = turboId,
            DiscountPercent = 10,
            StartsAt = start,
            EndsAt = end,
            IsActive = true
        })).Ok);

        Assert.True((await catalog.SaveCampaignAsync(new ProductCampaign
        {
            Name = "E2E Activation Second",
            ProductId = turboId,
            DiscountPercent = 20,
            StartsAt = start,
            EndsAt = end,
            IsActive = false
        })).Ok);

        var second = await db.ProductCampaigns.AsNoTracking()
            .SingleAsync(item => item.Name == "E2E Activation Second");
        var (ok, error) = await catalog.SetCampaignActiveAsync(second.Id, true);
        Assert.False(ok);
        Assert.Contains("already covers", error, StringComparison.OrdinalIgnoreCase);
        Assert.False(await db.ProductCampaigns.AsNoTracking()
            .Where(item => item.Id == second.Id)
            .Select(item => item.IsActive)
            .SingleAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public void Activate_forms_post_lowercase_true_false_not_razor_bools()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var index = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminCampaigns", "Index.cshtml"));
        var details = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminCampaigns", "Details.cshtml"));

        Assert.Contains("value=\"@(campaign.IsActive ? \"false\" : \"true\")\"", index, StringComparison.Ordinal);
        Assert.Contains("value=\"@(Model.IsActive ? \"false\" : \"true\")\"", details, StringComparison.Ordinal);
        Assert.DoesNotContain("value=\"@(!campaign.IsActive)\"", index, StringComparison.Ordinal);
        Assert.DoesNotContain("value=\"@(!Model.IsActive)\"", details, StringComparison.Ordinal);
        Assert.Contains("data-campaign-active-form", index, StringComparison.Ordinal);
        Assert.Contains("data-campaign-active-form", details, StringComparison.Ordinal);
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
