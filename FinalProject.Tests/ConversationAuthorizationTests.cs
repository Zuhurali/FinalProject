using FinalProject.Repositories.Implementations;
using FinalProject.Data;
using FinalProject.Models.Entities;
using FinalProject.MVC.Hubs;
using FinalProject.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FinalProject.Tests;

public class ConversationAuthorizationTests
{
    [Fact]
    public async Task Buyer_and_seller_can_view_and_reply()
    {
        await using var context = CreateContext();
        SeedStoreConversation(
            context,
            out var conversationId,
            out var buyerId,
            out var sellerId,
            out _);
        var service = CreateService(context);

        var buyerView = await service.GetConversationAsync(conversationId, buyerId, markRead: false);
        var sellerView = await service.GetConversationAsync(conversationId, sellerId, markRead: false);
        Assert.NotNull(buyerView);
        Assert.NotNull(sellerView);
        Assert.Single(buyerView!.Messages);

        var buyerReply = await service.ReplyAsync(conversationId, buyerId, "Still interested.");
        var sellerReply = await service.ReplyAsync(conversationId, sellerId, "Yes, available.");
        Assert.NotNull(buyerReply);
        Assert.NotNull(sellerReply);

        var after = await service.GetConversationAsync(conversationId, sellerId, markRead: false);
        Assert.Equal(3, after!.Messages.Count);
        Assert.Contains(after.Messages, message => message.Content == "Still interested.");
        Assert.Contains(after.Messages, message => message.Content == "Yes, available.");
    }

    [Fact]
    public async Task Unrelated_admin_cannot_list_open_or_reply_even_with_asStoreStaff()
    {
        await using var context = CreateContext();
        SeedStoreConversation(
            context,
            out var conversationId,
            out _,
            out var sellerId,
            out var adminId);
        var service = CreateService(context);
        var originalCount = await context.Messages.CountAsync(message => message.ConversationId == conversationId);
        var originalContents = await context.Messages
            .Where(message => message.ConversationId == conversationId)
            .Select(message => message.Content)
            .OrderBy(content => content)
            .ToListAsync();

        var adminList = await service.GetConversationsAsync(adminId, asStoreStaff: true);
        Assert.DoesNotContain(adminList, item => item.Id == conversationId);

        var adminView = await service.GetConversationAsync(
            conversationId,
            adminId,
            markRead: false,
            asStoreStaff: true);
        Assert.Null(adminView);

        var adminReply = await service.ReplyAsync(
            conversationId,
            adminId,
            "Admin should not send this.",
            asStoreStaff: true);
        Assert.Null(adminReply);

        Assert.Equal(
            originalCount,
            await context.Messages.CountAsync(message => message.ConversationId == conversationId));
        var afterContents = await context.Messages
            .Where(message => message.ConversationId == conversationId)
            .Select(message => message.Content)
            .OrderBy(content => content)
            .ToListAsync();
        Assert.Equal(originalContents, afterContents);

        // Actual store-inbox seller retains access.
        Assert.NotNull(await service.GetConversationAsync(conversationId, sellerId, markRead: false));
        Assert.Contains(
            await service.GetConversationsAsync(sellerId, asStoreStaff: false),
            item => item.Id == conversationId);
    }

    [Fact]
    public async Task SignalR_join_rejects_when_GetConversation_denies_access()
    {
        await using var context = CreateContext();
        SeedStoreConversation(
            context,
            out var conversationId,
            out _,
            out _,
            out var adminId);
        var service = CreateService(context);

        // ConversationHub.JoinConversation only joins after GetConversationAsync succeeds.
        var denied = await service.GetConversationAsync(
            conversationId,
            adminId,
            markRead: false,
            asStoreStaff: true);
        Assert.Null(denied);
        Assert.Equal($"conversation-{conversationId}", ConversationHub.GroupName(conversationId));
    }

    private static MarketplaceService CreateService(FinalProjectDbContext context) =>
        new(new FinalProjectRepository(context), new FinalProjectRepository(context), EmptyConfig(), new NotificationService(new FinalProjectRepository(context)));

    private static FinalProjectDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseInMemoryDatabase("conversation-auth-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new FinalProjectDbContext(options);
    }

    private static IConfiguration EmptyConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Marketplace:StoreInboxEmail"] = "ali.store@example.com"
            })
            .Build();

    private static void SeedStoreConversation(
        FinalProjectDbContext context,
        out int conversationId,
        out string buyerId,
        out string sellerId,
        out string adminId)
    {
        buyerId = "buyer-1";
        sellerId = "seller-store-inbox";
        adminId = "admin-unrelated";
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
                Email = "ali.store@example.com",
                UserName = "ali.store@example.com",
                FirstName = "Ali"
            },
            new IdentityUserReference
            {
                Id = adminId,
                Email = "other.admin@example.com",
                UserName = "other.admin@example.com",
                FirstName = "OtherAdmin"
            });
        context.Brands.Add(new Brand { Id = 200, Name = "AuthBrand", CreatedAt = now });
        context.Categories.Add(new Category { Id = 200, Name = "AuthCategory" });
        context.Cars.Add(new Car
        {
            Id = 200,
            BrandId = 200,
            CategoryId = 200,
            Model = "Auth Model",
            Year = 2024,
            Price = 20000,
            Mileage = 500,
            FuelType = "Petrol",
            Transmission = "Auto",
            Color = "Silver",
            Description = "Store listing",
            IsNew = true,
            CreatedAt = now,
            UpdatedAt = now,
            Status = "Active",
            OwnerId = null
        });
        var conversation = new Conversation
        {
            CarId = 200,
            BuyerId = buyerId,
            SellerId = sellerId,
            Subject = "Inquiry about AuthBrand Auth Model",
            CreatedAt = now,
            UpdatedAt = now
        };
        conversation.Messages.Add(new CarMessage
        {
            SenderId = buyerId,
            ReceiverId = sellerId,
            Content = "Is this car still available?",
            CreatedAt = now
        });
        context.Conversations.Add(conversation);
        context.SaveChanges();
        conversationId = conversation.Id;
    }
}
