using FinalProject.Data;
using FinalProject.Models.Entities;
using FinalProject.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FinalProject.Tests;

public class ConversationHidePersistenceTests
{
    [Fact]
    public async Task Hide_is_per_user_and_restores_when_new_message_arrives()
    {
        await using var context = CreateContext();
        SeedConversation(context, out var conversationId, out var buyerId, out var sellerId);
        var service = new MarketplaceService(context, EmptyConfig(), new NotificationService(context));

        Assert.True(await service.HideConversationAsync(conversationId, buyerId));

        var buyerList = await service.GetConversationsAsync(buyerId);
        var sellerList = await service.GetConversationsAsync(sellerId);
        Assert.DoesNotContain(buyerList, item => item.Id == conversationId);
        Assert.Contains(sellerList, item => item.Id == conversationId);

        var detailsForSeller = await service.GetConversationAsync(
            conversationId,
            sellerId,
            markRead: false);
        Assert.NotNull(detailsForSeller);
        Assert.Single(detailsForSeller!.Messages);

        var reply = await service.ReplyAsync(conversationId, sellerId, "Still available?");
        Assert.NotNull(reply);

        buyerList = await service.GetConversationsAsync(buyerId);
        sellerList = await service.GetConversationsAsync(sellerId);
        Assert.Contains(buyerList, item => item.Id == conversationId);
        Assert.Contains(sellerList, item => item.Id == conversationId);

        detailsForSeller = await service.GetConversationAsync(
            conversationId,
            sellerId,
            markRead: false);
        Assert.Equal(2, detailsForSeller!.Messages.Count);
        Assert.Equal(0, await context.ConversationHides.CountAsync());
    }

    [Fact]
    public async Task Unauthorized_user_cannot_hide_conversation()
    {
        await using var context = CreateContext();
        SeedConversation(context, out var conversationId, out _, out _);
        context.IdentityUsers.Add(new IdentityUserReference
        {
            Id = "stranger",
            Email = "stranger@example.com",
            UserName = "stranger@example.com"
        });
        await context.SaveChangesAsync();

        var service = new MarketplaceService(context, EmptyConfig(), new NotificationService(context));
        Assert.False(await service.HideConversationAsync(conversationId, "stranger"));
        Assert.Equal(0, await context.ConversationHides.CountAsync());
    }

    private static FinalProjectDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseInMemoryDatabase("conversation-hides-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new FinalProjectDbContext(options);
    }

    private static IConfiguration EmptyConfig() =>
        new ConfigurationBuilder().AddInMemoryCollection().Build();

    private static void SeedConversation(
        FinalProjectDbContext context,
        out int conversationId,
        out string buyerId,
        out string sellerId)
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
            Subject = "Inquiry about TestBrand Model X",
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
    }
}
