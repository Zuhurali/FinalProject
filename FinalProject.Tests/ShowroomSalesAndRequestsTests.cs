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
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb;Trusted_Connection=True;TrustServerCertificate=True";

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
    public void Admin_showroom_surfaces_require_Admin_or_SuperAdmin()
    {
        Assert.Equal("Admin,SuperAdmin", typeof(AdminRequestsController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Admin,SuperAdmin", typeof(AdminSalesController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Admin,SuperAdmin", typeof(AdminShowroomController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);

        var complete = typeof(AdminRequestsController).GetMethod(nameof(AdminRequestsController.CompleteSale))!;
        Assert.NotNull(complete.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(complete.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());

        var voidSale = typeof(AdminSalesController).GetMethod(nameof(AdminSalesController.Void))!;
        Assert.NotNull(voidSale.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(voidSale.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());

        Assert.NotNull(typeof(AdminShowroomController).GetMethod(nameof(AdminShowroomController.RecordSale))!
            .GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(typeof(AdminShowroomController).GetMethod(nameof(AdminShowroomController.VoidSale))!
            .GetCustomAttribute<HttpPostAttribute>());
    }

    [Fact]
    public async Task Purchase_request_persists_user_id_status_and_rejects_illegal_transition()
    {
        await using var db = CreateMvcDb();
        var catalog = new ShopCatalogService(db);
        var product = await db.ShopProducts.AsNoTracking().FirstAsync(item => item.IsActive && item.Quantity > 0);
        var email = $"req-{Guid.NewGuid():N}@example.com";
        var key = Guid.NewGuid();

        var created = await catalog.SavePurchaseRequestAsync(new ShowroomPurchaseRequest
        {
            ProductId = product.Id,
            CustomerName = "Showroom Tester",
            Email = email,
            Phone = "+994 50 111 22 33",
            Quantity = 1,
            SubmissionKey = key,
            UserId = "user-showroom-test"
        });
        Assert.True(created.Ok, created.Error);
        Assert.NotNull(created.RequestId);

        try
        {
            var saved = await catalog.GetPurchaseRequestAsync(created.RequestId!.Value);
            Assert.NotNull(saved);
            Assert.Equal(ShowroomRequestStatus.New, saved!.Status);
            Assert.Equal("user-showroom-test", saved.UserId);
            Assert.Equal(email, saved.Email);

            var (contacted, contactError) = await catalog.SetPurchaseRequestStatusAsync(
                saved.Id,
                ShowroomRequestStatus.Contacted);
            Assert.True(contacted, contactError);

            var (illegal, illegalError) = await catalog.SetPurchaseRequestStatusAsync(
                saved.Id,
                ShowroomRequestStatus.New);
            Assert.False(illegal);
            Assert.Contains("Cannot change status", illegalError);

            var forUser = await catalog.GetPurchaseRequestsForUserAsync("user-showroom-test");
            Assert.Contains(forUser, item => item.Id == saved.Id);
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM ShowroomPurchaseRequests WHERE Id = {created.RequestId};
                """);
        }
    }

    [Fact]
    public async Task Claim_orphaned_purchase_requests_by_account_email()
    {
        await using var db = CreateMvcDb();
        var catalog = new ShopCatalogService(db);
        var product = await db.ShopProducts.AsNoTracking().FirstAsync(item => item.IsActive && item.Quantity > 0);
        var email = $"orphan-{Guid.NewGuid():N}@example.com";

        var created = await catalog.SavePurchaseRequestAsync(new ShowroomPurchaseRequest
        {
            ProductId = product.Id,
            CustomerName = "Orphan Claim",
            Email = email,
            Phone = "+994 50 222 33 44",
            Quantity = 1,
            SubmissionKey = Guid.NewGuid()
        });
        Assert.True(created.Ok, created.Error);

        try
        {
            var claimed = await catalog.ClaimOrphanedPurchaseRequestsByEmailAsync("claim-user", email);
            Assert.Equal(1, claimed);
            var forUser = await catalog.GetPurchaseRequestsForUserAsync("claim-user");
            Assert.Contains(forUser, item => item.Id == created.RequestId);
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM ShowroomPurchaseRequests WHERE Id = {created.RequestId};
                """);
        }
    }

    [Fact]
    public async Task Inactive_product_cannot_receive_purchase_request()
    {
        await using var db = CreateMvcDb();
        var catalog = new ShopCatalogService(db);
        var product = await db.ShopProducts.FirstAsync(item => item.IsActive && item.Quantity > 0);
        product.IsActive = false;
        await db.SaveChangesAsync();

        try
        {
            var result = await catalog.SavePurchaseRequestAsync(new ShowroomPurchaseRequest
            {
                ProductId = product.Id,
                CustomerName = "Inactive Check",
                Email = $"inactive-{Guid.NewGuid():N}@example.com",
                Phone = "+994 50 333 44 55",
                Quantity = 1,
                SubmissionKey = Guid.NewGuid()
            });
            Assert.False(result.Ok);
            Assert.Equal("Product not found.", result.Error);
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE ShopProducts SET IsActive = CAST(1 AS bit) WHERE Id = {product.Id};
                """);
        }
    }

    [Fact]
    public async Task Record_sale_idempotency_key_prevents_duplicate_stock_decrement()
    {
        await using var db = CreateMvcDb();
        var catalog = new ShopCatalogService(db);
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
        var catalog = new ShopCatalogService(db);
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
    public async Task Complete_purchase_request_with_sale_updates_status_and_stock()
    {
        await using var db = CreateMvcDb();
        var catalog = new ShopCatalogService(db);
        var product = await db.ShopProducts.FirstAsync(item => item.IsActive && item.Quantity >= 3);
        var originalQty = product.Quantity;
        var email = $"complete-{Guid.NewGuid():N}@example.com";

        var created = await catalog.SavePurchaseRequestAsync(new ShowroomPurchaseRequest
        {
            ProductId = product.Id,
            CustomerName = "Complete Sale",
            Email = email,
            Phone = "+994 50 444 55 66",
            Quantity = 2,
            SubmissionKey = Guid.NewGuid()
        });
        Assert.True(created.Ok, created.Error);
        var requestId = created.RequestId!.Value;
        int? saleId = null;

        try
        {
            var (ok, error) = await catalog.CompletePurchaseRequestWithSaleAsync(
                requestId,
                "complete-admin",
                "e2e complete");
            Assert.True(ok, error);

            var request = await catalog.GetPurchaseRequestAsync(requestId);
            Assert.Equal(ShowroomRequestStatus.Completed, request!.Status);

            var after = await db.ShopProducts.AsNoTracking().SingleAsync(item => item.Id == product.Id);
            Assert.Equal(originalQty - 2, after.Quantity);

            var sale = await db.InPersonSales.AsNoTracking()
                .SingleAsync(item => item.Note == "e2e complete" && item.ProductId == product.Id);
            saleId = sale.Id;
            Assert.Equal(2, sale.Quantity);
            Assert.Equal(requestId, sale.PurchaseRequestId);
            Assert.Null(sale.VoidedAt);

            var (statusOnly, statusError) = await catalog.SetPurchaseRequestStatusAsync(
                requestId,
                ShowroomRequestStatus.Completed);
            Assert.False(statusOnly);
            Assert.Contains("Cannot change status", statusError);

            var (again, againError) = await catalog.CompletePurchaseRequestWithSaleAsync(
                requestId,
                "complete-admin",
                null);
            Assert.True(again, againError);
            Assert.Equal(1, await db.InPersonSales.CountAsync(item => item.PurchaseRequestId == requestId));
            var afterRetry = await db.ShopProducts.AsNoTracking().SingleAsync(item => item.Id == product.Id);
            Assert.Equal(originalQty - 2, afterRetry.Quantity);

            var (voided, voidError) = await catalog.VoidSaleAsync(sale.Id, "complete-admin");
            Assert.True(voided, voidError);
            var reopened = await catalog.GetPurchaseRequestAsync(requestId);
            Assert.Equal(ShowroomRequestStatus.Contacted, reopened!.Status);
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
                DELETE FROM ShowroomPurchaseRequests WHERE Id = {requestId};
                UPDATE ShopProducts
                SET Quantity = {originalQty},
                    InStock = CASE WHEN {originalQty} > 0 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
                WHERE Id = {product.Id};
                """);
        }
    }

    [Fact]
    public async Task Walk_in_sale_rejects_when_open_requests_reserve_remaining_stock()
    {
        await using var db = CreateMvcDb();
        var catalog = new ShopCatalogService(db);
        var product = await db.ShopProducts.FirstAsync(item =>
            item.IsActive && item.Quantity >= 2 && item.Quantity <= ShowroomRequestRules.MaxQuantityPerRequest);
        var originalQty = product.Quantity;
        var email = $"reserve-{Guid.NewGuid():N}@example.com";
        var note = "reserve-walkin-" + Guid.NewGuid().ToString("N");

        var created = await catalog.SavePurchaseRequestAsync(new ShowroomPurchaseRequest
        {
            ProductId = product.Id,
            CustomerName = "Reserve Guard",
            Email = email,
            Phone = "+994 50 555 66 77",
            Quantity = originalQty,
            SubmissionKey = Guid.NewGuid()
        });
        Assert.True(created.Ok, created.Error);

        try
        {
            var (ok, error) = await catalog.RecordSaleAsync(product.Id, 1, "reserve-admin", note);
            Assert.False(ok);
            Assert.Contains("reserved", error, StringComparison.OrdinalIgnoreCase);

            var after = await db.ShopProducts.AsNoTracking().SingleAsync(item => item.Id == product.Id);
            Assert.Equal(originalQty, after.Quantity);
            Assert.Equal(0, await db.InPersonSales.CountAsync(item => item.Note == note));
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM InPersonSales WHERE Note = {note};
                DELETE FROM ShowroomPurchaseRequests WHERE Id = {created.RequestId};
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
        var service = new ShowroomAdminService(db);
        var product = await db.ShopProducts.AsNoTracking()
            .FirstAsync(item => item.IsActive && item.Quantity >= 2);
        var originalQty = product.Quantity;
        var note = "api-sale-" + Guid.NewGuid().ToString("N");
        int? saleId = null;

        try
        {
            var idempotencyKey = Guid.NewGuid();
            var (sale, error) = await service.RecordSaleAsync(
                new RecordShowroomSaleRequest
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
                new RecordShowroomSaleRequest
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
