using FinalProject.Commerce;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.Models.Entities;
using FinalProject.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FinalProject.Tests;

/// <summary>
/// SQL Server integration checks for marketplace messaging, hides, and notifications.
/// Additive disposable rows only — existing conversations/messages/notifications are left intact.
/// </summary>
[Collection("CampaignSql")]
public class MessagingAndNotificationSqlPersistenceTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public async Task StartConversation_and_Reply_persist_messages_and_notifications_in_sql()
    {
        await using var db = CreateApiDb();
        var users = await RequireUsersAsync(db, 2);
        var buyerId = users[0];
        var sellerId = users[1];
        var marker = $"sql-msg-{Guid.NewGuid():N}";
        var carId = 0;
        var conversationId = 0;
        var firstMessageId = 0;
        var replyMessageId = 0;

        try
        {
            carId = await CreateDisposableActiveCarAsync(db, sellerId, marker);
            var marketplace = CreateMarketplace(db);

            var started = await marketplace.StartConversationAsync(
                carId,
                buyerId,
                new StartConversationRequestDto
                {
                    Subject = $"Inquiry {marker}",
                    Content = $"Is {marker} still available?"
                });
            Assert.NotNull(started);
            conversationId = started!.Id;
            firstMessageId = started.Messages.Single().Id;

            var conversationRow = await db.Conversations.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == conversationId);
            Assert.NotNull(conversationRow);
            Assert.Equal(carId, conversationRow!.CarId);
            Assert.Equal(buyerId, conversationRow.BuyerId);
            Assert.Equal(sellerId, conversationRow.SellerId);

            var messageRows = await db.Messages.AsNoTracking()
                .Where(item => item.ConversationId == conversationId)
                .OrderBy(item => item.Id)
                .ToListAsync();
            Assert.Single(messageRows);
            Assert.Equal(buyerId, messageRows[0].SenderId);
            Assert.Equal(sellerId, messageRows[0].ReceiverId);
            Assert.Contains(marker, messageRows[0].Content, StringComparison.Ordinal);

            var sellerNotify = await db.UserNotifications.AsNoTracking()
                .SingleOrDefaultAsync(item =>
                    item.UserId == sellerId
                    && item.Type == NotificationRules.MessageType
                    && item.RelatedMessageId == firstMessageId);
            Assert.NotNull(sellerNotify);
            Assert.Equal(conversationId, sellerNotify!.RelatedConversationId);
            Assert.False(sellerNotify.IsRead);
            Assert.Contains(marker, sellerNotify.Body, StringComparison.Ordinal);

            var reply = await marketplace.ReplyAsync(
                conversationId,
                sellerId,
                $"Yes, {marker} is available.");
            Assert.NotNull(reply);
            replyMessageId = reply!.Id;

            messageRows = await db.Messages.AsNoTracking()
                .Where(item => item.ConversationId == conversationId)
                .OrderBy(item => item.Id)
                .ToListAsync();
            Assert.Equal(2, messageRows.Count);
            Assert.Equal(sellerId, messageRows[1].SenderId);
            Assert.Equal(buyerId, messageRows[1].ReceiverId);

            var buyerNotify = await db.UserNotifications.AsNoTracking()
                .SingleOrDefaultAsync(item =>
                    item.UserId == buyerId
                    && item.Type == NotificationRules.MessageType
                    && item.RelatedMessageId == replyMessageId);
            Assert.NotNull(buyerNotify);
            Assert.Equal(conversationId, buyerNotify!.RelatedConversationId);
            Assert.Contains(marker, buyerNotify.Body, StringComparison.Ordinal);
        }
        finally
        {
            await CleanupMessagingAsync(db, carId, conversationId, marker);
        }
    }

    [Fact]
    public async Task HideConversation_survives_fresh_sql_dbcontext_for_that_user_only()
    {
        await using var setup = CreateApiDb();
        var users = await RequireUsersAsync(setup, 2);
        var buyerId = users[0];
        var sellerId = users[1];
        var marker = $"sql-hide-{Guid.NewGuid():N}";
        var carId = 0;
        var conversationId = 0;

        try
        {
            carId = await CreateDisposableActiveCarAsync(setup, sellerId, marker);
            var marketplace = CreateMarketplace(setup);
            var started = await marketplace.StartConversationAsync(
                carId,
                buyerId,
                new StartConversationRequestDto { Content = $"Hide check {marker}" });
            Assert.NotNull(started);
            conversationId = started!.Id;

            Assert.True(await marketplace.HideConversationAsync(conversationId, buyerId));

            var hideRow = await setup.ConversationHides.AsNoTracking()
                .SingleOrDefaultAsync(item =>
                    item.ConversationId == conversationId && item.UserId == buyerId);
            Assert.NotNull(hideRow);

            await setup.DisposeAsync();

            await using var reloaded = CreateApiDb();
            var reloadedMarketplace = CreateMarketplace(reloaded);
            var buyerList = await reloadedMarketplace.GetConversationsAsync(buyerId);
            var sellerList = await reloadedMarketplace.GetConversationsAsync(sellerId);

            Assert.DoesNotContain(buyerList, item => item.Id == conversationId);
            Assert.Contains(sellerList, item => item.Id == conversationId);
            Assert.True(await reloaded.ConversationHides.AsNoTracking().AnyAsync(item =>
                item.ConversationId == conversationId && item.UserId == buyerId));
        }
        finally
        {
            await using var cleanup = CreateApiDb();
            await CleanupMessagingAsync(cleanup, carId, conversationId, marker);
        }
    }

    [Fact]
    public async Task Stranger_cannot_hide_conversation_and_cannot_mark_foreign_notification_read()
    {
        await using var db = CreateApiDb();
        var users = await RequireUsersAsync(db, 3);
        var buyerId = users[0];
        var sellerId = users[1];
        var strangerId = users[2];
        var marker = $"sql-iso-{Guid.NewGuid():N}";
        var carId = 0;
        var conversationId = 0;

        try
        {
            carId = await CreateDisposableActiveCarAsync(db, sellerId, marker);
            var marketplace = CreateMarketplace(db);
            var notifications = new NotificationService(db);

            var started = await marketplace.StartConversationAsync(
                carId,
                buyerId,
                new StartConversationRequestDto { Content = $"Isolation {marker}" });
            Assert.NotNull(started);
            conversationId = started!.Id;
            var messageId = started.Messages.Single().Id;

            Assert.False(await marketplace.HideConversationAsync(conversationId, strangerId));
            Assert.False(await db.ConversationHides.AsNoTracking().AnyAsync(item =>
                item.ConversationId == conversationId && item.UserId == strangerId));

            var sellerNotify = await db.UserNotifications.AsNoTracking()
                .SingleAsync(item =>
                    item.UserId == sellerId
                    && item.RelatedMessageId == messageId
                    && item.Type == NotificationRules.MessageType);

            Assert.False(await notifications.MarkReadAsync(sellerNotify.Id, strangerId));
            Assert.False(await notifications.MarkReadAsync(sellerNotify.Id, buyerId));

            var stillUnread = await db.UserNotifications.AsNoTracking()
                .SingleAsync(item => item.Id == sellerNotify.Id);
            Assert.False(stillUnread.IsRead);

            Assert.True(await notifications.MarkReadAsync(sellerNotify.Id, sellerId));
            stillUnread = await db.UserNotifications.AsNoTracking()
                .SingleAsync(item => item.Id == sellerNotify.Id);
            Assert.True(stillUnread.IsRead);
        }
        finally
        {
            await CleanupMessagingAsync(db, carId, conversationId, marker);
        }
    }

    [Fact]
    public async Task Price_drop_notification_persists_in_UserNotifications_sql()
    {
        await using var db = CreateApiDb();
        var users = await RequireUsersAsync(db, 2);
        var ownerId = users[0];
        var fanId = users[1];
        var marker = $"sql-pricedrop-{Guid.NewGuid():N}";
        var carId = 0;
        var previousPrice = 42860m;
        var newPrice = 39900m;

        try
        {
            carId = await CreateDisposableActiveCarAsync(db, ownerId, marker, previousPrice);
            db.Favorites.Add(new Favorite
            {
                UserId = fanId,
                CarId = carId,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var notifications = new NotificationService(db);
            var created = await notifications.CreatePriceDropNotificationsAsync(
                carId,
                marker,
                previousPrice,
                newPrice);

            Assert.Single(created);
            Assert.Equal(fanId, created[0].UserId);
            Assert.Equal(NotificationRules.PriceDropType, created[0].Type);

            var fromSql = await db.UserNotifications.AsNoTracking()
                .SingleOrDefaultAsync(item =>
                    item.Id == created[0].Id
                    && item.UserId == fanId
                    && item.Type == NotificationRules.PriceDropType
                    && item.RelatedCarId == carId);
            Assert.NotNull(fromSql);
            Assert.False(fromSql!.IsRead);
            Assert.Contains(marker, fromSql.Body, StringComparison.Ordinal);
            Assert.Equal(
                NotificationRules.PriceDropDedupKey(carId, previousPrice, newPrice),
                fromSql.DedupKey);
        }
        finally
        {
            await CleanupPriceDropAsync(db, carId, marker);
        }
    }

    private static MarketplaceService CreateMarketplace(FinalProjectDbContext db) =>
        new(
            db,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Force private-listing seller = car.OwnerId (do not route to store inbox).
                ["Marketplace:StoreInboxEmail"] = "unused-sql-messaging@example.com"
            }).Build(),
            new NotificationService(db));

    private static async Task<IReadOnlyList<string>> RequireUsersAsync(
        FinalProjectDbContext db,
        int minimum)
    {
        var users = await db.IdentityUsers.AsNoTracking()
            .Select(user => user.Id)
            .Take(minimum)
            .ToListAsync();
        Assert.True(
            users.Count >= minimum,
            $"Need at least {minimum} AspNetUsers rows for disposable messaging SQL tests.");
        return users;
    }

    private static async Task<int> CreateDisposableActiveCarAsync(
        FinalProjectDbContext db,
        string ownerId,
        string marker,
        decimal price = 25000m)
    {
        var brandId = await db.Brands.AsNoTracking().Select(brand => brand.Id).FirstAsync();
        var categoryId = await db.Categories.AsNoTracking().Select(category => category.Id).FirstAsync();
        var now = DateTime.UtcNow;
        var car = new Car
        {
            BrandId = brandId,
            CategoryId = categoryId,
            Model = marker,
            Year = 2024,
            Price = price,
            Mileage = 1000,
            FuelType = "Petrol",
            Transmission = "Automatic",
            Color = "Black",
            Description = $"Disposable SQL messaging car {marker}",
            IsNew = false,
            IsFeatured = false,
            OwnerId = ownerId,
            Status = CarPublicListing.ActiveStatus,
            CreatedAt = now,
            UpdatedAt = now,
            Images =
            {
                new CarImage
                {
                    ImageUrl = "/assets/images/catalog/bmw/m340i.jpg",
                    IsMain = true,
                    DisplayOrder = 0
                }
            }
        };
        db.Cars.Add(car);
        await db.SaveChangesAsync();
        return car.Id;
    }

    private static async Task CleanupMessagingAsync(
        FinalProjectDbContext db,
        int carId,
        int conversationId,
        string marker)
    {
        if (conversationId > 0)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM UserNotifications
                WHERE RelatedConversationId = {conversationId}
                   OR (RelatedMessageId IS NOT NULL AND RelatedMessageId IN (
                        SELECT Id FROM Messages WHERE ConversationId = {conversationId}));
                DELETE FROM ConversationHides WHERE ConversationId = {conversationId};
                DELETE FROM Messages WHERE ConversationId = {conversationId};
                DELETE FROM Conversations WHERE Id = {conversationId};
                """);
        }

        if (carId > 0)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM UserNotifications WHERE RelatedCarId = {carId};
                DELETE FROM Favorites WHERE CarId = {carId};
                DELETE FROM CarImages WHERE CarId = {carId};
                DELETE FROM Cars WHERE Id = {carId} AND Model = {marker};
                """);
        }
    }

    private static async Task CleanupPriceDropAsync(
        FinalProjectDbContext db,
        int carId,
        string marker)
    {
        if (carId <= 0)
        {
            return;
        }

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM UserNotifications
            WHERE RelatedCarId = {carId}
              AND [Type] = {NotificationRules.PriceDropType};
            DELETE FROM Favorites WHERE CarId = {carId};
            DELETE FROM CarImages WHERE CarId = {carId};
            DELETE FROM Cars WHERE Id = {carId} AND Model = {marker};
            """);
    }

    private static FinalProjectDbContext CreateApiDb()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new FinalProjectDbContext(options);
    }
}
