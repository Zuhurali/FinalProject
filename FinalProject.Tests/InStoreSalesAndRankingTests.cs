using System.Reflection;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Controllers;
using FinalProject.MVC.Data;
using FinalProject.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests;

[Collection("CampaignSql")]
public class InStoreSalesAndRankingTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public void Admin_sales_recording_requires_Admin_or_SuperAdmin_and_post_antiforgery()
    {
        var controller = typeof(AdminSalesController);
        Assert.Equal("Admin,SuperAdmin", controller.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        var record = controller.GetMethod(nameof(AdminSalesController.Record))!;
        Assert.NotNull(record.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(record.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.Null(record.GetCustomAttribute<HttpGetAttribute>());
    }

    [Fact]
    public async Task RecordSaleAsync_reduces_stock_once_and_persists_non_demo_sale()
    {
        await using var db = CreateDb();
        var catalog = new ShopCatalogService(db);
        var product = await db.ShopProducts.FirstAsync(item => item.Quantity >= 2);
        var originalQty = product.Quantity;
        var note = "E2E genuine sale " + Guid.NewGuid().ToString("N");

        try
        {
            var (ok, error) = await catalog.RecordSaleAsync(product.Id, 2, "e2e-sales-test", note);
            Assert.True(ok, error);

            var sale = await db.InPersonSales.AsNoTracking().SingleAsync(item => item.Note == note);
            Assert.False(sale.IsDemo);
            Assert.Equal(2, sale.Quantity);
            Assert.Equal(product.Id, sale.ProductId);

            var after = await db.ShopProducts.AsNoTracking().SingleAsync(item => item.Id == product.Id);
            Assert.Equal(originalQty - 2, after.Quantity);

            var listed = await catalog.GetSalesAsync();
            Assert.Contains(listed, item => item.Id == sale.Id);
            Assert.DoesNotContain(listed, item => item.IsDemo);
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE ShopProducts
                SET Quantity = {originalQty},
                    InStock = CASE WHEN {originalQty} > 0 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
                WHERE Id = {product.Id};
                DELETE FROM InPersonSales WHERE Note = {note};
                """);
        }
    }

    [Fact]
    public async Task GetSalesAsync_excludes_confirmed_demo_rows()
    {
        await using var db = CreateDb();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var catalog = new ShopCatalogService(db);
        var product = await db.ShopProducts.AsNoTracking().Select(item => new { item.Id, item.Price }).FirstAsync();

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO InPersonSales (ProductId, Quantity, UnitPrice, RecordedByUserId, Note, SoldAt, IsDemo)
            VALUES (
                {product.Id},
                3,
                {product.Price},
                {PopularityDemoPlan.ReviewerId(1)},
                {PopularityDemoPlan.DemoSaleNote},
                {new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc)},
                CAST(1 AS bit));
            """);

        var listed = await catalog.GetSalesAsync();
        Assert.DoesNotContain(listed, sale => sale.IsDemo);
        Assert.DoesNotContain(listed, sale => sale.Note == PopularityDemoPlan.DemoSaleNote);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Popularity_apply_uses_recorded_sales_and_zero_when_empty()
    {
        await using var db = CreateDb();
        var popularity = new ProductPopularityService(db);
        var catalog = new ShopCatalogService(db);
        var products = (await catalog.GetAllAsync()).Take(3).ToList();
        Assert.NotEmpty(products);

        await popularity.ApplyAsync(products);

        foreach (var product in products)
        {
            Assert.True(product.RatingCount >= 0);
            Assert.True(product.ReviewCount >= 0);
            Assert.True(product.RealSalesCount >= 0);
            Assert.Equal(0, product.DemoSalesCount);
            Assert.Equal(product.RealSalesCount, product.SalesCount);
            if (product.RatingCount == 0)
            {
                Assert.Equal(0, product.Rating);
            }
        }
    }

    [Fact]
    public async Task Demo_cleanup_removes_only_isdemo_presentation_rows()
    {
        await using var db = CreateDb();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var product = await db.ShopProducts.AsNoTracking().Select(item => new { item.Id, item.Price }).FirstAsync();

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO InPersonSales (ProductId, Quantity, UnitPrice, RecordedByUserId, Note, SoldAt, IsDemo)
            VALUES (
                {product.Id}, 2, {product.Price},
                {PopularityDemoPlan.ReviewerId(2)},
                {PopularityDemoPlan.DemoSaleNote},
                {new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc)},
                CAST(1 AS bit));
            """);
        var genuineNote = "Keep genuine " + Guid.NewGuid().ToString("N");
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO InPersonSales (ProductId, Quantity, UnitPrice, RecordedByUserId, Note, SoldAt, IsDemo)
            VALUES (
                {product.Id}, 1, {product.Price},
                {"real-admin-user"},
                {genuineNote},
                {DateTime.UtcNow},
                CAST(0 AS bit));
            """);

        Assert.True(await db.InPersonSales.CountAsync(sale => sale.IsDemo && sale.Note == PopularityDemoPlan.DemoSaleNote) >= 1);

        var removed = await db.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM InPersonSales
            WHERE IsDemo = 1
              AND (
                    Note = {PopularityDemoPlan.DemoSaleNote}
                 OR RecordedByUserId LIKE {"demo-popularity-reviewer-%"}
              );
            """);
        Assert.True(removed >= 1);
        Assert.Equal(0, await db.InPersonSales.CountAsync(sale => sale.IsDemo && sale.Note == PopularityDemoPlan.DemoSaleNote));
        Assert.Equal(1, await db.InPersonSales.CountAsync(sale => sale.Note == genuineNote && !sale.IsDemo));

        await transaction.RollbackAsync();
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new ApplicationDbContext(options);
    }
}
