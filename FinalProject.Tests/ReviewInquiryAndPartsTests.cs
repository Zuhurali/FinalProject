using FinalProject.MVC.Repositories.Implementations;
using FinalProject.Repositories.Implementations;
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
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb_Test;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public async Task Author_edits_keep_history_and_other_users_cannot_delete()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new FinalProjectDbContext(options);
        var reviews = new ProductReviewService(new FinalProjectRepository(db), new FinalProjectRepository(db));

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
    public async Task Inquiry_reply_survives_close_and_reopen_until_a_new_reply_is_submitted()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new ApplicationDbContext(options);
        var catalog = new ShopCatalogService(new ApplicationRepository(db));
        var productId = await db.ShopProducts.AsNoTracking().Select(product => product.Id).FirstAsync();
        var memberId = await db.Users.AsNoTracking().Select(user => user.Id).FirstAsync();
        var email = $"reply-check-{Guid.NewGuid():N}@autovault.local";
        var (saved, saveError) = await catalog.SaveInquiryAsync(new ProductInquiry
        {
            ProductId = productId,
            CustomerName = "Reply Check",
            Email = email,
            Message = "Is this part available in the store?",
            UserId = memberId
        });
        Assert.True(saved, saveError);

        var inquiry = await db.ProductInquiries.AsNoTracking()
            .Where(item => item.Email == email)
            .OrderByDescending(item => item.Id)
            .FirstAsync();
        Assert.Equal(ProductInquiry.LabelAwaitingReply, inquiry.StatusLabel);
        Assert.Null(inquiry.AdminReply);

        try
        {
            var (replied, replyError) = await catalog.SaveInquiryReplyAsync(inquiry.Id, "Yes, it is on the shelf.", "superadmin-1");
            Assert.True(replied, replyError);

            var answered = await db.ProductInquiries.AsNoTracking().SingleAsync(item => item.Id == inquiry.Id);
            Assert.Equal("Yes, it is on the shelf.", answered.AdminReply);
            Assert.Equal("superadmin-1", answered.AdminUserId);
            Assert.NotNull(answered.AdminRepliedAt);
            Assert.Equal("Answered", answered.StatusLabel);
            Assert.False(answered.IsHandled);
            var firstRepliedAt = answered.AdminRepliedAt;
            var notesAfterReply = await CountReplyNotificationsAsync(db, inquiry.Id);
            Assert.Equal(1, notesAfterReply);

            Assert.True(await catalog.SetInquiryHandledAsync(inquiry.Id, true));
            var closed = await db.ProductInquiries.AsNoTracking().SingleAsync(item => item.Id == inquiry.Id);
            Assert.Equal("Closed", closed.StatusLabel);
            Assert.Equal("Yes, it is on the shelf.", closed.AdminReply);
            Assert.Equal("superadmin-1", closed.AdminUserId);
            Assert.Equal(firstRepliedAt, closed.AdminRepliedAt);
            Assert.Equal(notesAfterReply, await CountReplyNotificationsAsync(db, inquiry.Id));

            Assert.True(await catalog.SetInquiryHandledAsync(inquiry.Id, false));
            var reopened = await db.ProductInquiries.AsNoTracking().SingleAsync(item => item.Id == inquiry.Id);
            Assert.Equal(ProductInquiry.LabelAwaitingFollowUp, reopened.StatusLabel);
            Assert.Equal("Yes, it is on the shelf.", reopened.AdminReply);
            Assert.Equal("superadmin-1", reopened.AdminUserId);
            Assert.Equal(firstRepliedAt, reopened.AdminRepliedAt);
            Assert.Equal(notesAfterReply, await CountReplyNotificationsAsync(db, inquiry.Id));
            Assert.Contains(await catalog.GetInquiriesForUserAsync(memberId), item => item.Id == inquiry.Id && item.AdminReply == "Yes, it is on the shelf.");
            Assert.Contains(await catalog.GetInquiriesAsync(ProductInquiry.LabelAwaitingFollowUp), item => item.Id == inquiry.Id);

            var (updated, updateError) = await catalog.SaveInquiryReplyAsync(inquiry.Id, "Yes, one is available today.", "superadmin-1");
            Assert.True(updated, updateError);
            var replaced = await db.ProductInquiries.AsNoTracking().SingleAsync(item => item.Id == inquiry.Id);
            Assert.Equal("Yes, one is available today.", replaced.AdminReply);
            Assert.DoesNotContain("Yes, it is on the shelf.", replaced.AdminReply);
            Assert.Equal("superadmin-1", replaced.AdminUserId);
            Assert.True(replaced.AdminRepliedAt > firstRepliedAt);
            Assert.Equal(ProductInquiry.LabelAnswered, replaced.StatusLabel);
            Assert.Equal(2, await CountReplyNotificationsAsync(db, inquiry.Id));
            Assert.Contains("AutoVault Support", await LatestReplyNotificationBodyAsync(db, inquiry.Id));

            Assert.True(await catalog.SetInquiryHandledAsync(inquiry.Id, true));
            var (followUp, followUpError) = await catalog.SaveInquiryAsync(new ProductInquiry
            {
                ProductId = productId,
                CustomerName = "Reply Check",
                Email = email,
                Message = "Can I collect it tomorrow morning?",
                UserId = memberId
            });
            Assert.True(followUp, followUpError);
            Assert.Equal(1, await db.ProductInquiries.CountAsync(item => item.Email == email));
            var followed = await db.ProductInquiries.AsNoTracking().SingleAsync(item => item.Id == inquiry.Id);
            Assert.Contains("Is this part available in the store?", followed.Message);
            Assert.Contains("Can I collect it tomorrow morning?", followed.Message);
            Assert.Equal("Yes, one is available today.", followed.AdminReply);
            Assert.DoesNotContain("Yes, it is on the shelf.", followed.AdminReply);
            Assert.Equal(ProductInquiry.LabelAwaitingFollowUp, followed.StatusLabel);
            Assert.Equal("superadmin-1", followed.AdminUserId);
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM UserNotifications WHERE DedupKey LIKE {("inquiry_reply:" + inquiry.Id + ":%")};
                """);
            await catalog.DeleteInquiryAsync(inquiry.Id);
        }
    }

    private static async Task<int> CountReplyNotificationsAsync(ApplicationDbContext db, int inquiryId)
    {
        var prefix = "inquiry_reply:" + inquiryId + ":%";
        var counts = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM UserNotifications WHERE DedupKey LIKE {prefix}").ToListAsync();
        return counts.Single();
    }

    private static async Task<string> LatestReplyNotificationBodyAsync(ApplicationDbContext db, int inquiryId)
    {
        var prefix = "inquiry_reply:" + inquiryId + ":%";
        var bodies = await db.Database.SqlQuery<string>($"SELECT TOP 1 Body AS [Value] FROM UserNotifications WHERE DedupKey LIKE {prefix} ORDER BY CreatedAt DESC").ToListAsync();
        return bodies.Single();
    }

    [Fact]
    public async Task Parts_pages_report_the_rows_actually_returned()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new FinalProjectDbContext(options);
        var parts = new PartsAdminService(new FinalProjectRepository(db));
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
