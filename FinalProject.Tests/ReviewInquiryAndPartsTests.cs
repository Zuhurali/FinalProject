using FinalProject.Data;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using FinalProject.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FinalProject.Tests;

[Collection("CampaignSql")]
public class ReviewInquiryAndPartsTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public async Task Author_edits_keep_history_and_other_users_cannot_delete()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new FinalProjectDbContext(options);
        var reviews = new ProductReviewService(db);

        var userId = await db.IdentityUsers.AsNoTracking().Select(user => user.Id).FirstAsync();
        var taken = await db.ProductReviews.AsNoTracking()
            .Where(review => review.UserId == userId)
            .Select(review => review.ProductId)
            .ToListAsync();
        var productId = await db.ShopProducts.AsNoTracking()
            .Select(product => product.Id)
            .FirstAsync(id => !taken.Contains(id));

        var (created, createError) = await reviews.SaveReviewAsync(productId, null, userId, "Original review text.");
        Assert.True(created != null, createError);
        Assert.False(created!.WasEdited);

        var (updated, updateError) = await reviews.SaveReviewAsync(productId, created.Id, userId, "Updated review text.");
        Assert.True(updated != null, updateError);
        Assert.True(updated!.WasEdited);
        Assert.Equal("Updated review text.", updated.Content);

        var history = await reviews.GetHistoryAsync(created.Id);
        Assert.NotNull(history);
        Assert.Equal("Original review text.", Assert.Single(history!).PreviousContent);

        var hidden = await reviews.SetHiddenAsync(created.Id, true);
        Assert.True(hidden);
        var stored = await db.ProductReviews.AsNoTracking().SingleAsync(review => review.Id == created.Id);
        Assert.Equal("Updated review text.", stored.Content);
        Assert.True(stored.IsEdited);
        Assert.True(stored.IsHidden);

        var denied = await reviews.DeleteReviewAsync(created.Id, "different-user");
        Assert.False(denied.Ok);
        Assert.NotNull(await db.ProductReviews.AsNoTracking().SingleOrDefaultAsync(review => review.Id == created.Id));

        var removed = await reviews.DeleteReviewAsync(created.Id, userId);
        Assert.True(removed.Ok, removed.Error);
        Assert.Empty(await db.ProductReviewRevisions.Where(revision => revision.ReviewId == created.Id).ToListAsync());
    }

    [Fact]
    public async Task Inquiry_reply_is_saved_and_reopening_clears_it()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new ApplicationDbContext(options);
        var catalog = new ShopCatalogService(db);
        var productId = await db.ShopProducts.AsNoTracking().Select(product => product.Id).FirstAsync();
        var (saved, saveError) = await catalog.SaveInquiryAsync(new ProductInquiry
        {
            ProductId = productId,
            CustomerName = "Reply Check",
            Email = "reply-check@autovault.local",
            Message = "Is this part available in the store?"
        });
        Assert.True(saved, saveError);

        var inquiry = await db.ProductInquiries.AsNoTracking()
            .Where(item => item.Email == "reply-check@autovault.local")
            .OrderByDescending(item => item.Id)
            .FirstAsync();
        var (replied, replyError) = await catalog.SaveInquiryReplyAsync(inquiry.Id, "Yes, it is on the shelf.", "admin-test");
        Assert.True(replied, replyError);

        var answered = await db.ProductInquiries.AsNoTracking().SingleAsync(item => item.Id == inquiry.Id);
        Assert.Equal("Yes, it is on the shelf.", answered.AdminReply);
        Assert.Equal("Answered", answered.StatusLabel);

        var (edited, editError) = await catalog.SaveInquiryReplyAsync(inquiry.Id, "Yes, one is available today.", "admin-test");
        Assert.True(edited, editError);
        Assert.Equal(1, await db.ProductInquiries.CountAsync(item => item.Email == "reply-check@autovault.local"));
        answered = await db.ProductInquiries.AsNoTracking().SingleAsync(item => item.Id == inquiry.Id);
        Assert.Equal("Yes, one is available today.", answered.AdminReply);

        Assert.True(await catalog.SetInquiryHandledAsync(inquiry.Id, false));
        var reopened = await db.ProductInquiries.AsNoTracking().SingleAsync(item => item.Id == inquiry.Id);
        Assert.Null(reopened.AdminReply);
        Assert.Equal("New", reopened.StatusLabel);

        Assert.True(await catalog.DeleteInquiryAsync(inquiry.Id));
        Assert.Empty(await db.ProductInquiries.Where(item => item.Id == inquiry.Id).ToListAsync());
    }

    [Fact]
    public async Task Parts_pages_report_the_rows_actually_returned()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new FinalProjectDbContext(options);
        var parts = new PartsAdminService(db);
        var first = await parts.ListAsync(null, null, null, 1, 10);
        var second = await parts.ListAsync(null, null, null, 2, 10);
        var third = await parts.ListAsync(null, null, null, 3, 10);
        var beyond = await parts.ListAsync(null, null, null, 99, 10);

        var firstWindow = PageWindow.Describe(first.Page, first.PageSize, first.Items.Count, first.Total);
        var secondWindow = PageWindow.Describe(second.Page, second.PageSize, second.Items.Count, second.Total);
        var thirdWindow = PageWindow.Describe(third.Page, third.PageSize, third.Items.Count, third.Total);

        Assert.Equal(1, firstWindow.Start);
        Assert.Equal(first.Items.Count, firstWindow.End - firstWindow.Start + 1);
        if (first.Total > 10)
        {
            Assert.Equal(11, secondWindow.Start);
            Assert.Equal(second.Items.Count, secondWindow.End - secondWindow.Start + 1);
        }

        if (first.Total > 20)
        {
            Assert.Equal(21, thirdWindow.Start);
            Assert.Equal(first.Total, thirdWindow.End);
            Assert.Equal(first.Total - 20, third.Items.Count);
        }

        Assert.Equal(Math.Max(1, (int)Math.Ceiling(first.Total / 10d)), beyond.Page);
        Assert.True(beyond.Items.Count <= 10);
    }
}
