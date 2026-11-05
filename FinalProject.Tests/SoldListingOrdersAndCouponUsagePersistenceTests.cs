using System.Reflection;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.MVC.Controllers;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using FinalProject.Services;
using FinalProject.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ApiCarPublicListing = FinalProject.Commerce.CarPublicListing;

namespace FinalProject.Tests;

/// <summary>
/// SQL Server integration checks for sold listing, checkout orders Admin surface, and coupon usage visibility.
/// Additive disposable rows only — existing catalog/CMS/order data is left intact.
/// </summary>
[Collection("CampaignSql")]
public class SoldListingOrdersAndCouponUsagePersistenceTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public void SoldCar_and_ShopThree_sources_use_sql_catalog_not_hardcoded_ids()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var carsController = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "CarsController.cs"));
        var soldView = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Cars", "SoldCar.cshtml"));
        var shopController = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "ShopController.cs"));

        Assert.Contains("public async Task<IActionResult> SoldCar()", carsController, StringComparison.Ordinal);
        Assert.Contains("CreateSoldQuery()", carsController, StringComparison.Ordinal);
        Assert.Contains("CarPublicListing.SoldStatus", carsController, StringComparison.Ordinal);
        Assert.Contains("@model IReadOnlyList<Car>", soldView, StringComparison.Ordinal);
        Assert.DoesNotContain("Audi A5 Coupe 45 TFSI", soldView, StringComparison.Ordinal);
        Assert.DoesNotContain("new[] { 1, 8, 3, 9, 5, 7, 10, 11, 4, 16, 6, 2 }", shopController, StringComparison.Ordinal);
        Assert.Contains("OrderBy(product => product.Name", shopController, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_orders_surface_is_legacy_archive_for_Admin_or_SuperAdmin()
    {
        Assert.Equal("Admin,SuperAdmin", typeof(AdminOrdersController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.NotNull(typeof(AdminOrdersController).GetMethod(nameof(AdminOrdersController.Index)));
        Assert.NotNull(typeof(AdminOrdersController).GetMethod(nameof(AdminOrdersController.Details)));
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var view = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminOrders", "Index.cshtml"));
        Assert.Contains("Legacy order archive", view, StringComparison.Ordinal);
        Assert.Contains("does <strong>not</strong> take online payment", view, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QueryAsync_with_Sold_status_returns_only_sold_rows_from_sql()
    {
        await using var db = CreateApiDb();
        var soldIds = await db.Cars.AsNoTracking()
            .Where(car => car.Status == "Sold")
            .Select(car => car.Id)
            .ToListAsync();
        var service = new CarService(db, new SilentNotifications());

        var page = await service.QueryAsync(new CarListQueryDto
        {
            Status = ApiCarPublicListing.SoldStatus,
            Page = 1,
            PageSize = 50
        });

        Assert.All(page.Items, item => Assert.Equal("Sold", item.Status, StringComparer.OrdinalIgnoreCase));
        Assert.Equal(soldIds.Count, page.TotalCount);
        foreach (var id in soldIds.Take(20))
        {
            Assert.Contains(page.Items, item => item.Id == id);
        }
    }

    [Fact]
    public async Task Orders_and_coupon_usages_are_readable_from_sql_for_admin_surfaces()
    {
        await using var db = CreateMvcDb();
        var orders = new OrderService(db, new CouponService(db));
        var coupons = new CouponService(db);

        var recent = await orders.GetRecentOrdersAsync(take: 25);
        var sqlOrderCount = await db.Orders.AsNoTracking().CountAsync();
        Assert.Equal(Math.Min(25, sqlOrderCount), recent.Count);
        Assert.Equal(
            await db.Orders.AsNoTracking()
                .OrderByDescending(order => order.OrderDate)
                .ThenByDescending(order => order.Id)
                .Take(Math.Min(25, sqlOrderCount))
                .Select(order => order.Id)
                .ToListAsync(),
            recent.Select(order => order.Id).ToList());

        var usageCounts = await coupons.GetUsageCountsByCodeAsync();
        var sqlGrouped = await db.CouponUsages.AsNoTracking()
            .GroupBy(usage => usage.CouponCode)
            .Select(group => new { Code = group.Key, Count = group.Count() })
            .ToListAsync();
        Assert.Equal(sqlGrouped.Count, usageCounts.Count);
        foreach (var row in sqlGrouped)
        {
            Assert.True(usageCounts.TryGetValue(row.Code, out var count));
            Assert.Equal(row.Count, count);
        }

        var recentUsages = await coupons.GetRecentUsagesAsync(take: 20);
        Assert.Equal(Math.Min(20, await db.CouponUsages.AsNoTracking().CountAsync()), recentUsages.Count);
    }

    [Fact]
    public async Task User_notification_persists_to_sql_and_can_be_marked_read()
    {
        await using var db = CreateApiDb();
        var userId = await db.IdentityUsers.AsNoTracking().Select(user => user.Id).FirstOrDefaultAsync();
        Assert.False(string.IsNullOrWhiteSpace(userId));

        var notifications = new NotificationService(db);
        var marker = $"sql-notify-{Guid.NewGuid():N}";
        var created = await notifications.CreateMessageNotificationAsync(
            userId!,
            "SqlPersistence",
            conversationId: 0,
            messageId: unchecked((int)(DateTime.UtcNow.Ticks & 0x7fffffff)),
            preview: marker);

        Assert.NotNull(created);
        try
        {
            var fromSql = await db.UserNotifications.AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == created!.Id);
            Assert.NotNull(fromSql);
            Assert.Equal(userId, fromSql!.UserId);
            Assert.Contains(marker, fromSql.Body, StringComparison.Ordinal);
            Assert.False(fromSql.IsRead);

            Assert.True(await notifications.MarkReadAsync(created!.Id, userId!));
            fromSql = await db.UserNotifications.AsNoTracking()
                .FirstAsync(item => item.Id == created.Id);
            Assert.True(fromSql.IsRead);
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM UserNotifications WHERE Id = {created!.Id};
                """);
        }
    }

    private static FinalProjectDbContext CreateApiDb()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new FinalProjectDbContext(options);
    }

    private static ApplicationDbContext CreateMvcDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class SilentNotifications : INotificationService
    {
        public Task<NotificationListResponseDto> GetForUserAsync(string userId, int take = 50, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NotificationUnreadCountsResponseDto> GetUnreadCountsAsync(string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NotificationResponseDto?> CreateMessageNotificationAsync(
            string receiverId,
            string senderDisplayName,
            int conversationId,
            int messageId,
            string preview,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<NotificationResponseDto>> CreatePriceDropNotificationsAsync(
            int carId,
            string carName,
            decimal previousPrice,
            decimal newPrice,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NotificationResponseDto>>(Array.Empty<NotificationResponseDto>());

        public Task<bool> MarkReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> MarkAllReadAsync(string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
