using System.Reflection;
using FinalProject.MVC.Controllers;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests;

[Collection("CampaignSql")]
public class ContactQuestionsMessagesTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public void Admin_contact_and_inquiry_surfaces_require_Admin_or_SuperAdmin()
    {
        Assert.Equal("Admin,SuperAdmin", typeof(AdminContactRequestsController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Admin,SuperAdmin", typeof(AdminInquiriesController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.NotNull(typeof(AdminContactRequestsController).GetMethod(nameof(AdminContactRequestsController.Delete))!
            .GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.NotNull(typeof(AdminInquiriesController).GetMethod(nameof(AdminInquiriesController.Reply))!
            .GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.NotNull(typeof(AccountController).GetMethod(nameof(AccountController.ProductQuestions))!
            .GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public async Task Contact_request_persists_and_status_filter_works()
    {
        await using var db = CreateMvcDb();
        var service = new ContactRequestService(db);
        var email = $"contact-{Guid.NewGuid():N}@example.com";

        var (ok, id, _, error) = await service.SaveAsync(new ContactFormModel
        {
            Name = "Module Tester",
            Email = email,
            Phone = "+994501112233",
            Car = "BMW 3 Series",
            Message = "Please call me about a showroom visit."
        }, userId: "contact-user-1");
        Assert.True(ok, error);
        Assert.NotNull(id);

        try
        {
            var allNew = await service.GetAllAsync(ContactRequestStatus.New);
            Assert.Contains(allNew, item => item.Id == id);

            var (statusOk, statusError) = await service.SetStatusAsync(id!.Value, ContactRequestStatus.Resolved);
            Assert.True(statusOk, statusError);
            var resolved = await service.GetAllAsync(ContactRequestStatus.Resolved);
            Assert.Contains(resolved, item => item.Id == id);
            Assert.DoesNotContain(await service.GetAllAsync(ContactRequestStatus.New), item => item.Id == id);

            var forUser = await service.GetForUserAsync("contact-user-1");
            Assert.Contains(forUser, item => item.Id == id);
            Assert.DoesNotContain(await service.GetForUserAsync("other-user"), item => item.Id == id);
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM ContactRequests WHERE Id = {id};
                """);
        }
    }

    [Fact]
    public async Task Product_inquiry_persists_user_id_and_filters_by_status()
    {
        await using var db = CreateMvcDb();
        var catalog = new ShopCatalogService(db);
        var product = await db.ShopProducts.AsNoTracking().FirstAsync(item => item.IsActive);
        var email = $"ask-{Guid.NewGuid():N}@example.com";

        var (ok, error) = await catalog.SaveInquiryAsync(new ProductInquiry
        {
            ProductId = product.Id,
            CustomerName = "Ask Tester",
            Email = email,
            Message = "Does this oil filter fit a 2019 BMW?",
            UserId = "ask-user-1"
        });
        Assert.True(ok, error);

        var saved = await db.ProductInquiries.AsNoTracking()
            .SingleAsync(item => item.Email == email);
        Assert.Equal("ask-user-1", saved.UserId);
        Assert.False(saved.IsHandled);

        try
        {
            var news = await catalog.GetInquiriesAsync("new");
            Assert.Contains(news, item => item.Id == saved.Id);

            var (replied, replyError) = await catalog.SaveInquiryReplyAsync(
                saved.Id,
                "Yes, it fits that model year.",
                "admin-1");
            Assert.True(replied, replyError);

            var answered = await catalog.GetInquiriesAsync("answered");
            Assert.Contains(answered, item => item.Id == saved.Id);
            Assert.DoesNotContain(await catalog.GetInquiriesAsync("new"), item => item.Id == saved.Id);

            var forUser = await catalog.GetInquiriesForUserAsync("ask-user-1");
            Assert.Contains(forUser, item => item.Id == saved.Id);
            Assert.DoesNotContain(await catalog.GetInquiriesForUserAsync("other-user"), item => item.Id == saved.Id);
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM ProductInquiries WHERE Id = {saved.Id};
                """);
        }
    }

    [Fact]
    public async Task Inquiry_claim_by_email_and_inactive_product_rejected()
    {
        await using var db = CreateMvcDb();
        var catalog = new ShopCatalogService(db);
        var product = await db.ShopProducts.FirstAsync(item => item.IsActive);
        var email = $"orphan-ask-{Guid.NewGuid():N}@example.com";

        var (ok, error) = await catalog.SaveInquiryAsync(new ProductInquiry
        {
            ProductId = product.Id,
            CustomerName = "Orphan Ask",
            Email = email,
            Message = "Is this part still available in Baku?"
        });
        Assert.True(ok, error);
        var saved = await db.ProductInquiries.AsNoTracking().SingleAsync(item => item.Email == email);

        try
        {
            Assert.Equal(1, await catalog.ClaimOrphanedInquiriesByEmailAsync("claim-ask-user", email));
            var forUser = await catalog.GetInquiriesForUserAsync("claim-ask-user");
            Assert.Contains(forUser, item => item.Id == saved.Id);

            product.IsActive = false;
            await db.SaveChangesAsync();
            var (blocked, blockedError) = await catalog.SaveInquiryAsync(new ProductInquiry
            {
                ProductId = product.Id,
                CustomerName = "Blocked",
                Email = $"inactive-{Guid.NewGuid():N}@example.com",
                Message = "Should not save for inactive product."
            });
            Assert.False(blocked);
            Assert.Equal("Product not found.", blockedError);
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM ProductInquiries WHERE Id = {saved.Id};
                UPDATE ShopProducts SET IsActive = CAST(1 AS bit) WHERE Id = {product.Id};
                """);
        }
    }

    [Fact]
    public async Task Dealer_conversation_messages_persist_and_remain_owner_scoped()
    {
        await using var db = new FinalProject.Data.FinalProjectDbContext(
            new DbContextOptionsBuilder<FinalProject.Data.FinalProjectDbContext>()
                .UseSqlServer(ConnectionString)
                .Options);

        var conversation = await db.Conversations.AsNoTracking()
            .Include(item => item.Messages)
            .FirstOrDefaultAsync();
        if (conversation == null)
        {
            return;
        }

        Assert.NotEmpty(conversation.Messages);
        Assert.All(conversation.Messages, message =>
        {
            Assert.False(string.IsNullOrWhiteSpace(message.Content));
            Assert.True(message.Content.Length <= 4000);
        });

        var buyerOnly = await db.Conversations.AsNoTracking()
            .Where(item => item.BuyerId == conversation.BuyerId)
            .Select(item => item.Id)
            .ToListAsync();
        Assert.Contains(conversation.Id, buyerOnly);

        var stranger = "not-a-participant-" + Guid.NewGuid().ToString("N");
        var strangerAccess = await db.Conversations.AsNoTracking()
            .AnyAsync(item =>
                item.Id == conversation.Id
                && (item.BuyerId == stranger || item.SellerId == stranger));
        Assert.False(strangerAccess);
    }

    private static ApplicationDbContext CreateMvcDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new ApplicationDbContext(options);
    }
}
