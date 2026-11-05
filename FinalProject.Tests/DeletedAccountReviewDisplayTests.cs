using FinalProject.Commerce;
using FinalProject.Data;
using FinalProject.Models.Entities;
using FinalProject.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests;

public class DeletedAccountReviewDisplayTests
{
    [Fact]
    public async Task Active_member_reviews_show_normal_author_name()
    {
        await using var db = await SeedAsync();
        var reviews = new ProductReviewService(db);

        var list = await reviews.GetReviewsAsync(1, currentUserId: null, includeHidden: false);
        var active = Assert.Single(list!, item => item.Content == "Great brake pads.");

        Assert.Equal("Sam Active", active.UserDisplayName);
        Assert.Equal("active-user", active.UserId);
        Assert.DoesNotContain("Deleted Account", active.UserDisplayName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Deleted_member_reviews_show_Deleted_Account_and_keep_content_stars_and_dates()
    {
        await using var db = await SeedAsync();
        var reviews = new ProductReviewService(db);
        var ratings = new ProductRatingService(db);

        var stored = await db.ProductReviews.AsNoTracking()
            .SingleAsync(review => review.UserId == "deleted-user");
        var list = await reviews.GetReviewsAsync(1, currentUserId: null, includeHidden: false);
        var deleted = Assert.Single(list!, item => item.Content == "Solid value for money.");

        Assert.Equal(ReviewContentRules.DeletedAccountDisplayName, deleted.UserDisplayName);
        Assert.Equal(stored.Content, deleted.Content);
        Assert.Equal(stored.CreatedAt, deleted.CreatedAt);
        Assert.Equal(stored.UpdatedAt, deleted.UpdatedAt);
        Assert.Equal(string.Empty, deleted.UserId);

        // Original author id remains on the persisted review row.
        Assert.Equal("deleted-user", stored.UserId);
        Assert.False(string.IsNullOrWhiteSpace(stored.Content));

        var summary = await ratings.GetProductRatingsAsync(1);
        Assert.NotNull(summary);
        Assert.Equal(2, summary!.TotalCount);
        Assert.Equal(4.5, summary.AverageRating);
        Assert.Contains(summary.Ratings, item => item.UserId == "deleted-user" && item.Rating == 5);
    }

    [Fact]
    public async Task Public_review_output_does_not_expose_deleted_user_personal_information()
    {
        await using var db = await SeedAsync();
        var reviews = new ProductReviewService(db);

        var list = await reviews.GetReviewsAsync(1, currentUserId: null, includeHidden: false);
        var deleted = Assert.Single(list!, item => item.Content == "Solid value for money.");

        Assert.Equal(ReviewContentRules.DeletedAccountDisplayName, deleted.UserDisplayName);
        Assert.DoesNotContain("Dana", deleted.UserDisplayName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Gone", deleted.UserDisplayName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deleted@example.com", deleted.UserDisplayName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deleted-user", deleted.UserDisplayName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, deleted.UserId);
        Assert.DoesNotContain("@", deleted.UserDisplayName, StringComparison.Ordinal);

        var payload = System.Text.Json.JsonSerializer.Serialize(deleted);
        Assert.DoesNotContain("deleted@example.com", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Dana", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("555-0199", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Product_average_rating_still_includes_deleted_account_ratings()
    {
        await using var db = await SeedAsync();
        var ratings = new ProductRatingService(db);

        var summary = await ratings.GetProductRatingsAsync(1);
        Assert.Equal(2, summary!.TotalCount);
        // (4 + 5) / 2 = 4.5 — deleted author's 5-star rating remains in the average.
        Assert.Equal(4.5, summary.AverageRating);

        var aggregates = await ratings.GetSummariesAsync([1]);
        Assert.Equal(4.5, Assert.Single(aggregates).AverageRating);
        Assert.Equal(2, aggregates[0].TotalCount);
    }

    private static async Task<FinalProjectDbContext> SeedAsync()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseInMemoryDatabase("deleted-review-display-" + Guid.NewGuid().ToString("N"))
            .Options;
        var db = new FinalProjectDbContext(options);

        db.ShopProducts.Add(new ShopProductReference
        {
            Id = 1,
            Name = "Brake Pads",
            Price = 49.99m,
            Quantity = 10,
            Brand = "AutoVault",
            Category = "Brakes",
            ImagePath = "/img/brake.png",
            IsActive = true,
            InStock = true,
            CreatedAt = DateTime.UtcNow
        });

        db.IdentityUsers.AddRange(
            new IdentityUserReference
            {
                Id = "active-user",
                UserName = "sam.active",
                Email = "sam@example.com",
                FirstName = "Sam",
                LastName = "Active",
                IsDeleted = false
            },
            new IdentityUserReference
            {
                Id = "deleted-user",
                UserName = "dana.gone",
                Email = "deleted@example.com",
                FirstName = "Dana",
                LastName = "Gone",
                IsDeleted = true
            });

        var createdActive = new DateTime(2024, 3, 10, 12, 0, 0, DateTimeKind.Utc);
        var createdDeleted = new DateTime(2024, 4, 15, 9, 30, 0, DateTimeKind.Utc);

        db.ProductReviews.AddRange(
            new ProductReview
            {
                Id = 1,
                ProductId = 1,
                UserId = "active-user",
                Content = "Great brake pads.",
                CreatedAt = createdActive,
                UpdatedAt = createdActive
            },
            new ProductReview
            {
                Id = 2,
                ProductId = 1,
                UserId = "deleted-user",
                Content = "Solid value for money.",
                CreatedAt = createdDeleted,
                UpdatedAt = createdDeleted
            });

        db.ProductRatings.AddRange(
            new ProductRating
            {
                Id = 1,
                ProductId = 1,
                UserId = "active-user",
                Rating = 4,
                CreatedAt = createdActive,
                UpdatedAt = createdActive
            },
            new ProductRating
            {
                Id = 2,
                ProductId = 1,
                UserId = "deleted-user",
                Rating = 5,
                CreatedAt = createdDeleted,
                UpdatedAt = createdDeleted
            });

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return db;
    }
}
