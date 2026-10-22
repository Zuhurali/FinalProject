using FinalProject.Commerce;
using FinalProject.Data;
using FinalProject.Models.Entities;
using FinalProject.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FinalProject.Tests;

public class NotificationServiceTests
{
    [Fact]
    public async Task Message_notification_skips_sender_and_dedupes_by_message_id()
    {
        await using var context = CreateContext();
        SeedConversation(context, out var conversationId, out var buyerId, out var sellerId, out var messageId);
        var notifications = new NotificationService(context);

        var first = await notifications.CreateMessageNotificationAsync(
            sellerId,
            "Buyer",
            conversationId,
            messageId,
            "Is this available?");
        var duplicate = await notifications.CreateMessageNotificationAsync(
            sellerId,
            "Buyer",
            conversationId,
            messageId,
            "Is this available?");

        Assert.NotNull(first);
        Assert.Null(duplicate);
        Assert.Equal(1, await context.UserNotifications.CountAsync());
        Assert.Equal(sellerId, first!.UserId);
        Assert.Equal(NotificationRules.MessageType, first.Type);
        Assert.Contains("/Account/Messages/", first.Url);
        Assert.Equal(1, await notifications.GetUnreadCountAsync(sellerId));
        Assert.Equal(0, await notifications.GetUnreadCountAsync(buyerId));
    }

    [Fact]
    public async Task Mark_read_is_scoped_to_owner()
    {
        await using var context = CreateContext();
        SeedConversation(context, out var conversationId, out var buyerId, out var sellerId, out var messageId);
        var notifications = new NotificationService(context);
        var created = await notifications.CreateMessageNotificationAsync(
            sellerId,
            "Buyer",
            conversationId,
            messageId,
            "Hello");

        Assert.False(await notifications.MarkReadAsync(created!.Id, buyerId));
        Assert.True(await notifications.MarkReadAsync(created.Id, sellerId));
        Assert.Equal(0, await notifications.GetUnreadCountAsync(sellerId));
    }

    [Fact]
    public async Task Unread_counts_are_split_by_menu_category()
    {
        await using var context = CreateContext();
        SeedConversation(context, out var conversationId, out _, out var sellerId, out var messageId);
        SeedCarWithFavorites(context, out var carId, out _, out var fanA, out _);
        var notifications = new NotificationService(context);

        await notifications.CreateMessageNotificationAsync(
            sellerId,
            "Buyer",
            conversationId,
            messageId,
            "Hello");
        await notifications.CreatePriceDropNotificationsAsync(carId, "Audi A5", 42860m, 39900m);

        var sellerCounts = await notifications.GetUnreadCountsAsync(sellerId);
        Assert.Equal(1, sellerCounts.Count);
        Assert.Equal(1, sellerCounts.Messages);
        Assert.Equal(0, sellerCounts.Favorites);

        var fanCounts = await notifications.GetUnreadCountsAsync(fanA);
        Assert.Equal(1, fanCounts.Count);
        Assert.Equal(0, fanCounts.Messages);
        Assert.Equal(1, fanCounts.Favorites);
    }

    [Fact]
    public async Task Price_drop_notifies_favoriters_once_and_ignores_increases()
    {
        await using var context = CreateContext();
        SeedCarWithFavorites(context, out var carId, out var ownerId, out var fanA, out var fanB);
        var notifications = new NotificationService(context);

        var increase = await notifications.CreatePriceDropNotificationsAsync(
            carId,
            "Audi A5",
            39900m,
            42860m);
        Assert.Empty(increase);

        var first = await notifications.CreatePriceDropNotificationsAsync(
            carId,
            "Audi A5",
            42860m,
            39900m);
        var second = await notifications.CreatePriceDropNotificationsAsync(
            carId,
            "Audi A5",
            42860m,
            39900m);

        Assert.Equal(2, first.Count);
        Assert.Empty(second);
        Assert.All(first, item => Assert.Contains("Audi A5", item.Body));
        Assert.Contains(first, item => item.UserId == fanA);
        Assert.Contains(first, item => item.UserId == fanB);
        Assert.DoesNotContain(first, item => item.UserId == ownerId);
        Assert.Equal(2, await context.UserNotifications.CountAsync());
    }

    [Fact]
    public async Task Reply_creates_receiver_notification_not_sender()
    {
        await using var context = CreateContext();
        SeedConversation(context, out var conversationId, out var buyerId, out var sellerId, out _);
        var notifications = new NotificationService(context);
        var marketplace = new MarketplaceService(context, EmptyConfig(), notifications);

        var reply = await marketplace.ReplyAsync(conversationId, sellerId, "Still available?");
        Assert.NotNull(reply);
        Assert.NotNull(reply!.Notification);
        Assert.Equal(buyerId, reply.Notification!.UserId);
        Assert.Equal(0, await notifications.GetUnreadCountAsync(sellerId));
        Assert.Equal(1, await notifications.GetUnreadCountAsync(buyerId));
    }

    [Fact]
    public async Task Car_update_creates_price_drop_notifications_only_on_decrease()
    {
        await using var context = CreateContext();
        SeedCarWithFavorites(context, out var carId, out _, out var fanA, out _);
        var notifications = new NotificationService(context);
        var cars = new CarService(context, notifications);

        var samePrice = await cars.UpdateAsync(carId, SaveCar(42860m));
        Assert.NotNull(samePrice);
        Assert.Empty(samePrice!.Notifications);

        var drop = await cars.UpdateAsync(carId, SaveCar(39900m));
        Assert.NotNull(drop);
        Assert.Contains(drop!.Notifications, item => item.UserId == fanA);
        Assert.Contains("Special offer!", drop.Notifications[0].Body);

        var again = await cars.UpdateAsync(carId, SaveCar(39900m));
        Assert.NotNull(again);
        Assert.Empty(again!.Notifications);
    }

    private static FinalProject.DTOs.SaveCarRequest SaveCar(decimal price) =>
        new()
        {
            BrandId = 200,
            CategoryId = 200,
            Model = "A5",
            Year = 2022,
            Price = price,
            Mileage = 12000,
            FuelType = "Petrol",
            Transmission = "Auto",
            Color = "Black",
            Description = "Test",
            ImageUrl = "/images/cars/a5.jpg",
            Status = "Active"
        };

    private static FinalProjectDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseInMemoryDatabase("notifications-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new FinalProjectDbContext(options);
    }

    private static IConfiguration EmptyConfig() =>
        new ConfigurationBuilder().AddInMemoryCollection().Build();

    private static void SeedConversation(
        FinalProjectDbContext context,
        out int conversationId,
        out string buyerId,
        out string sellerId,
        out int messageId)
    {
        buyerId = "buyer-1";
        sellerId = "seller-1";
        var now = DateTime.UtcNow;

        context.IdentityUsers.AddRange(
            new IdentityUserReference
            {
                Id = buyerId,
                Email = "buyer@example.com",
                UserName = "buyer@example.com",
                FirstName = "Buyer"
            },
            new IdentityUserReference
            {
                Id = sellerId,
                Email = "seller@example.com",
                UserName = "seller@example.com",
                FirstName = "Seller"
            });
        context.Brands.Add(new Brand { Id = 100, Name = "TestBrand", CreatedAt = now });
        context.Categories.Add(new Category { Id = 100, Name = "TestCategory" });
        context.Cars.Add(new Car
        {
            Id = 100,
            BrandId = 100,
            CategoryId = 100,
            Model = "Model X",
            Year = 2024,
            Price = 10000,
            Mileage = 1000,
            FuelType = "Petrol",
            Transmission = "Auto",
            Color = "Black",
            Description = "Test",
            IsNew = true,
            CreatedAt = now,
            UpdatedAt = now,
            Status = "Active",
            OwnerId = sellerId
        });
        var conversation = new Conversation
        {
            CarId = 100,
            BuyerId = buyerId,
            SellerId = sellerId,
            Subject = "Inquiry",
            CreatedAt = now,
            UpdatedAt = now
        };
        conversation.Messages.Add(new CarMessage
        {
            SenderId = buyerId,
            ReceiverId = sellerId,
            Content = "Is this available?",
            CreatedAt = now
        });
        context.Conversations.Add(conversation);
        context.SaveChanges();
        conversationId = conversation.Id;
        messageId = conversation.Messages.Single().Id;
    }

    private static void SeedCarWithFavorites(
        FinalProjectDbContext context,
        out int carId,
        out string ownerId,
        out string fanA,
        out string fanB)
    {
        ownerId = "owner-1";
        fanA = "fan-a";
        fanB = "fan-b";
        var now = DateTime.UtcNow;

        context.IdentityUsers.AddRange(
            new IdentityUserReference { Id = ownerId, Email = "owner@example.com", UserName = "owner@example.com" },
            new IdentityUserReference { Id = fanA, Email = "a@example.com", UserName = "a@example.com" },
            new IdentityUserReference { Id = fanB, Email = "b@example.com", UserName = "b@example.com" });
        context.Brands.Add(new Brand { Id = 200, Name = "Audi", CreatedAt = now });
        context.Categories.Add(new Category { Id = 200, Name = "Coupe" });
        var car = new Car
        {
            Id = 200,
            BrandId = 200,
            CategoryId = 200,
            Model = "A5",
            Year = 2022,
            Price = 42860,
            Mileage = 12000,
            FuelType = "Petrol",
            Transmission = "Auto",
            Color = "Black",
            Description = "Test",
            IsNew = false,
            CreatedAt = now,
            UpdatedAt = now,
            Status = "Active",
            OwnerId = ownerId
        };
        context.Cars.Add(car);
        context.Favorites.AddRange(
            new Favorite { UserId = fanA, CarId = 200, CreatedAt = now },
            new Favorite { UserId = fanB, CarId = 200, CreatedAt = now });
        context.SaveChanges();
        carId = car.Id;
    }
}
