using FinalProject.MVC.Repositories.Implementations;
using FinalProject.Repositories.Implementations;
using System.Reflection;
using FinalProject.Controllers;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Controllers;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using FinalProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests;

[Collection("CampaignSql")]
public class ShowroomSalesAndRequestsTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb_Test;Trusted_Connection=True;TrustServerCertificate=True";

    [Theory]
    [InlineData(ShowroomRequestStatus.New, ShowroomRequestStatus.Contacted, true)]
    [InlineData(ShowroomRequestStatus.New, ShowroomRequestStatus.Completed, false)]
    [InlineData(ShowroomRequestStatus.New, ShowroomRequestStatus.Cancelled, true)]
    [InlineData(ShowroomRequestStatus.Contacted, ShowroomRequestStatus.Completed, false)]
    [InlineData(ShowroomRequestStatus.Contacted, ShowroomRequestStatus.Cancelled, true)]
    [InlineData(ShowroomRequestStatus.Contacted, ShowroomRequestStatus.New, false)]
    [InlineData(ShowroomRequestStatus.Completed, ShowroomRequestStatus.Cancelled, false)]
    [InlineData(ShowroomRequestStatus.Cancelled, ShowroomRequestStatus.New, false)]
    public void Status_transitions_follow_showroom_rules(string from, string to, bool allowed)
    {
        Assert.Equal(allowed, ShowroomRequestRules.CanTransition(from, to));
    }

    [Fact]
    public void AllowedNextStatuses_hides_terminal_dropdowns_and_limits_new_options()
    {
        Assert.Equal(
            new[] { ShowroomRequestStatus.New, ShowroomRequestStatus.Cancelled },
            ShowroomRequestRules.AllowedNextStatuses(ShowroomRequestStatus.New));
        Assert.Empty(ShowroomRequestRules.AllowedNextStatuses(ShowroomRequestStatus.Completed));
        Assert.Empty(ShowroomRequestRules.AllowedNextStatuses(ShowroomRequestStatus.Cancelled));
        Assert.DoesNotContain(
            ShowroomRequestStatus.Completed,
            ShowroomRequestRules.AllowedNextStatuses(ShowroomRequestStatus.New));
    }

    [Fact]
    public void Admin_showroom_surfaces_require_Admin_or_SuperAdmin()
    {
        Assert.Equal("Admin,SuperAdmin", typeof(AdminSalesController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Admin,SuperAdmin", typeof(AdminShowroomController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);

        var voidSale = typeof(AdminSalesController).GetMethod(nameof(AdminSalesController.Void))!;
        Assert.NotNull(voidSale.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(voidSale.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());

        Assert.NotNull(typeof(AdminShowroomController).GetMethod(nameof(AdminShowroomController.RecordSale))!
            .GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(typeof(AdminShowroomController).GetMethod(nameof(AdminShowroomController.VoidSale))!
            .GetCustomAttribute<HttpPostAttribute>());
    }

    [Fact]
    public async Task Record_sale_idempotency_key_prevents_duplicate_stock_decrement()
    {
        await using var db = CreateMvcDb();
        var catalog = new ShopCatalogService(new ApplicationRepository(db));
        var product = await db.ShopProducts.FirstAsync(item => item.IsActive && item.Quantity >= 2);
        var originalQty = product.Quantity;
        var key = Guid.NewGuid();
        var note = "idempotent-sale-" + key.ToString("N");

        try
        {
            var (first, firstError) = await catalog.RecordSaleAsync(product.Id, 1, "idem-admin", note, key);
            Assert.True(first, firstError);
            var (second, secondError) = await catalog.RecordSaleAsync(product.Id, 1, "idem-admin", note, key);
            Assert.True(second, secondError);

            var after = await db.ShopProducts.AsNoTracking().SingleAsync(item => item.Id == product.Id);
            Assert.Equal(originalQty - 1, after.Quantity);
            Assert.Equal(1, await db.InPersonSales.CountAsync(item => item.IdempotencyKey == key.ToString("N")));
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM InPersonSales WHERE Note = {note};
                UPDATE ShopProducts
                SET Quantity = {originalQty},
                    InStock = CASE WHEN {originalQty} > 0 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
                WHERE Id = {product.Id};
                """);
        }
    }

    [Fact]
    public async Task Void_sale_restores_stock_atomically_and_excludes_from_list()
    {
        await using var db = CreateMvcDb();
        var catalog = new ShopCatalogService(new ApplicationRepository(db));
        var product = await db.ShopProducts.FirstAsync(item => item.IsActive && item.Quantity >= 2);
        var originalQty = product.Quantity;
        var note = "void-sale-" + Guid.NewGuid().ToString("N");

        try
        {
            var (ok, error) = await catalog.RecordSaleAsync(product.Id, 1, "void-admin", note);
            Assert.True(ok, error);
            var sale = await db.InPersonSales.AsNoTracking().SingleAsync(item => item.Note == note);
            var afterSale = await db.ShopProducts.AsNoTracking().SingleAsync(item => item.Id == product.Id);
            Assert.Equal(originalQty - 1, afterSale.Quantity);

            var (voided, voidError) = await catalog.VoidSaleAsync(sale.Id, "void-admin");
            Assert.True(voided, voidError);

            var restored = await db.ShopProducts.AsNoTracking().SingleAsync(item => item.Id == product.Id);
            Assert.Equal(originalQty, restored.Quantity);

            var listed = await catalog.GetSalesAsync();
            Assert.DoesNotContain(listed, item => item.Id == sale.Id);

            var (again, againError) = await catalog.VoidSaleAsync(sale.Id, "void-admin");
            Assert.False(again);
            Assert.Contains("already voided", againError);
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM InPersonSales WHERE Note = {note};
                UPDATE ShopProducts
                SET Quantity = {originalQty},
                    InStock = CASE WHEN {originalQty} > 0 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
                WHERE Id = {product.Id};
                """);
        }
    }

    [Fact]
    public async Task Api_showroom_service_records_and_voids_sale_against_sql_server()
    {
        await using var db = CreateApiDb();
        var service = new ShowroomAdminService(new FinalProjectRepository(db));
        var product = await db.ShopProducts.AsNoTracking()
            .FirstAsync(item => item.IsActive && item.Quantity >= 2);
        var originalQty = product.Quantity;
        var note = "api-sale-" + Guid.NewGuid().ToString("N");
        int? saleId = null;

        try
        {
            var idempotencyKey = Guid.NewGuid();
            var (sale, error) = await service.RecordSaleAsync(
                new RecordShowroomSaleRequestDto
                {
                    ProductId = product.Id,
                    Quantity = 1,
                    Note = note,
                    IdempotencyKey = idempotencyKey
                },
                "api-admin");
            Assert.NotNull(sale);
            Assert.Null(error);
            saleId = sale!.Id;

            var (retry, retryError) = await service.RecordSaleAsync(
                new RecordShowroomSaleRequestDto
                {
                    ProductId = product.Id,
                    Quantity = 1,
                    Note = note,
                    IdempotencyKey = idempotencyKey
                },
                "api-admin");
            Assert.NotNull(retry);
            Assert.Null(retryError);
            Assert.Equal(sale.Id, retry!.Id);

            var after = await db.ShopProducts.AsNoTracking().SingleAsync(item => item.Id == product.Id);
            Assert.Equal(originalQty - 1, after.Quantity);

            var listed = await service.ListSalesAsync();
            Assert.Contains(listed, item => item.Id == sale.Id);

            var (voidOk, voidError) = await service.VoidSaleAsync(sale.Id, "api-admin");
            Assert.True(voidOk, voidError);
            var restored = await db.ShopProducts.AsNoTracking().SingleAsync(item => item.Id == product.Id);
            Assert.Equal(originalQty, restored.Quantity);
        }
        finally
        {
            if (saleId.HasValue)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    DELETE FROM InPersonSales WHERE Id = {saleId.Value};
                    """);
            }

            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE ShopProducts
                SET Quantity = {originalQty},
                    InStock = CASE WHEN {originalQty} > 0 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
                WHERE Id = {product.Id};
                """);
        }
    }

    private static ApplicationDbContext CreateMvcDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static FinalProjectDbContext CreateApiDb()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new FinalProjectDbContext(options);
    }
}
