using System.Reflection;
using FinalProject.Controllers;
using FinalProject.Data;
using FinalProject.Models.Entities;
using FinalProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FinalProject.Tests;

[Collection("CampaignSql")]
public class FavoritesReviewsRatingsTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public void Marketplace_favorite_endpoints_require_authorization()
    {
        Assert.NotNull(typeof(MarketplaceController).GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(typeof(MarketplaceController)
            .GetMethod(nameof(MarketplaceController.AddProductFavorite))!
            .GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(typeof(MarketplaceController)
            .GetMethod(nameof(MarketplaceController.RemoveProductFavorite))!
            .GetCustomAttribute<HttpDeleteAttribute>());
        Assert.Equal("Admin,SuperAdmin",
            typeof(ProductReviewsController)
                .GetMethod(nameof(ProductReviewsController.AdminDelete))!
                .GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }

    [Fact]
    public async Task Product_favorite_persists_dedupes_and_hides_inactive()
    {
        await using var db = CreateDb();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Marketplace:StoreInboxEmail"] = "unused@example.com"
        }).Build();
        var marketplace = new MarketplaceService(db, config, new NotificationService(db));

        var userId = await db.IdentityUsers.AsNoTracking().Select(user => user.Id).FirstAsync();
        var product = await db.ShopProducts.FirstAsync(item => item.IsActive && item.Quantity >= 0);
        var originalActive = product.IsActive;

        try
        {
            var first = await marketplace.AddProductFavoriteAsync(userId, product.Id);
            Assert.NotNull(first);
            var second = await marketplace.AddProductFavoriteAsync(userId, product.Id);
            Assert.NotNull(second);
            Assert.Equal(first!.Id, second!.Id);

            var count = await db.ProductFavorites.CountAsync(
                item => item.UserId == userId && item.ProductId == product.Id);
            Assert.Equal(1, count);

            var listed = await marketplace.GetProductFavoritesAsync(userId);
            Assert.Contains(listed, item => item.ProductId == product.Id);

            var ids = await marketplace.GetProductFavoriteIdsAsync(userId);
            Assert.Contains(product.Id, ids);

            product.IsActive = false;
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var afterHide = await marketplace.GetProductFavoritesAsync(userId);
            Assert.DoesNotContain(afterHide, item => item.ProductId == product.Id);
            Assert.Null(await marketplace.AddProductFavoriteAsync(userId, product.Id));

            product = await db.ShopProducts.SingleAsync(item => item.Id == product.Id);
            product.IsActive = true;
            await db.SaveChangesAsync();

            Assert.True(await marketplace.RemoveProductFavoriteAsync(userId, product.Id));
            Assert.False(await marketplace.RemoveProductFavoriteAsync(userId, product.Id));
            Assert.Empty(await db.ProductFavorites.Where(
                item => item.UserId == userId && item.ProductId == product.Id).ToListAsync());
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM ProductFavorites WHERE UserId = {userId} AND ProductId = {product.Id};
                UPDATE ShopProducts SET IsActive = {(originalActive ? 1 : 0)} WHERE Id = {product.Id};
                """);
        }
    }

    [Fact]
    public async Task Product_favorite_rejects_unknown_product_id()
    {
        await using var db = CreateDb();
        var userId = await db.IdentityUsers.AsNoTracking().Select(user => user.Id).FirstAsync();
        var missingProductId = int.MaxValue - 42;

        db.ProductFavorites.Add(new ProductFavorite
        {
            UserId = userId,
            ProductId = missingProductId,
            CreatedAt = DateTime.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.False(await db.ProductFavorites.AnyAsync(
            item => item.UserId == userId && item.ProductId == missingProductId));
    }

    [Fact]
    public async Task Product_favorite_is_scoped_to_owner()
    {
        await using var db = CreateDb();
        var config = new ConfigurationBuilder().Build();
        var marketplace = new MarketplaceService(db, config, new NotificationService(db));
        var users = await db.IdentityUsers.AsNoTracking().Select(user => user.Id).Take(2).ToListAsync();
        Assert.True(users.Count >= 2);
        var product = await db.ShopProducts.AsNoTracking().FirstAsync(item => item.IsActive);

        var added = await marketplace.AddProductFavoriteAsync(users[0], product.Id);
        Assert.NotNull(added);

        try
        {
            var otherIds = await marketplace.GetProductFavoriteIdsAsync(users[1]);
            Assert.DoesNotContain(product.Id, otherIds);
            Assert.False(await marketplace.RemoveProductFavoriteAsync(users[1], product.Id));
            Assert.True(await db.ProductFavorites.AnyAsync(
                item => item.UserId == users[0] && item.ProductId == product.Id));
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM ProductFavorites WHERE ProductId = {product.Id} AND UserId = {users[0]};
                """);
        }
    }

    [Fact]
    public async Task Rating_and_review_require_real_shop_product_row()
    {
        await using var db = CreateDb();
        var ratings = new ProductRatingService(db);
        var reviews = new ProductReviewService(db);
        var missingId = int.MaxValue - 7;
        var userId = await db.IdentityUsers.AsNoTracking().Select(user => user.Id).FirstAsync();

        Assert.Null(await ratings.GetProductRatingsAsync(missingId));
        Assert.Null(await ratings.SaveRatingAsync(missingId, userId, 5));
        Assert.Null(await reviews.GetReviewsAsync(missingId, userId, includeHidden: false));
        var (created, _) = await reviews.SaveReviewAsync(missingId, null, userId, "Should not persist without product.");
        Assert.Null(created);
    }

    [Fact]
    public async Task Rating_persists_rejects_duplicate_and_inactive_product()
    {
        await using var db = CreateDb();
        var ratings = new ProductRatingService(db);
        var userId = await db.IdentityUsers.AsNoTracking().Select(user => user.Id).FirstAsync();
        var product = await db.ShopProducts.FirstAsync(item => item.IsActive);
        var originalActive = product.IsActive;

        // Clear any prior rating for this pair.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM ProductRatings WHERE ProductId = {product.Id} AND UserId = {userId};
            """);

        try
        {
            var saved = await ratings.SaveRatingAsync(product.Id, userId, 4);
            Assert.NotNull(saved);
            Assert.False(saved!.AlreadyExists);
            Assert.Equal(4, saved.Rating);

            var duplicate = await ratings.SaveRatingAsync(product.Id, userId, 5);
            Assert.NotNull(duplicate);
            Assert.True(duplicate!.AlreadyExists);
            Assert.Equal(4, duplicate.Rating);

            var summary = await ratings.GetProductRatingsAsync(product.Id);
            Assert.NotNull(summary);
            Assert.True(summary!.TotalCount >= 1);
            Assert.Contains(summary.Ratings, item => item.UserId == userId && item.Rating == 4);

            var mine = await ratings.GetUserRatingAsync(product.Id, userId);
            Assert.NotNull(mine);
            Assert.Equal(saved.Id, mine!.Id);

            product.IsActive = false;
            await db.SaveChangesAsync();
            Assert.Null(await ratings.SaveRatingAsync(product.Id, userId, 3));

            product = await db.ShopProducts.SingleAsync(item => item.Id == product.Id);
            product.IsActive = true;
            await db.SaveChangesAsync();

            var (found, allowed) = await ratings.DeleteRatingByUserAsync(product.Id, saved.Id, "other-user");
            Assert.True(found);
            Assert.False(allowed);

            var (foundOwn, allowedOwn) = await ratings.DeleteRatingByUserAsync(product.Id, saved.Id, userId);
            Assert.True(foundOwn);
            Assert.True(allowedOwn);
            Assert.Empty(await db.ProductRatings.Where(
                item => item.ProductId == product.Id && item.UserId == userId).ToListAsync());
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM ProductRatings WHERE ProductId = {product.Id} AND UserId = {userId};
                UPDATE ShopProducts SET IsActive = {(originalActive ? 1 : 0)} WHERE Id = {product.Id};
                """);
        }
    }

    [Fact]
    public async Task Review_create_rejected_for_inactive_product_but_existing_can_edit()
    {
        await using var db = CreateDb();
        var reviews = new ProductReviewService(db);
        var userId = await db.IdentityUsers.AsNoTracking().Select(user => user.Id).FirstAsync();
        var taken = await db.ProductReviews.AsNoTracking()
            .Where(review => review.UserId == userId)
            .Select(review => review.ProductId)
            .ToListAsync();
        var product = await db.ShopProducts.FirstAsync(item => item.IsActive && !taken.Contains(item.Id));
        var clean = await db.ShopProducts.FirstAsync(
            item => item.Id != product.Id && item.IsActive && !taken.Contains(item.Id));
        var originalActive = product.IsActive;
        int? reviewId = null;

        try
        {
            var (created, createError) = await reviews.SaveReviewAsync(
                product.Id,
                null,
                userId,
                "Favorite module review text.");
            Assert.True(created != null, createError);
            reviewId = created!.Id;

            product.IsActive = false;
            await db.SaveChangesAsync();

            var (edited, editError) = await reviews.SaveReviewAsync(
                product.Id,
                reviewId,
                userId,
                "Edited while product inactive.");
            Assert.True(edited != null, editError);
            Assert.Equal("Edited while product inactive.", edited!.Content);

            clean.IsActive = false;
            await db.SaveChangesAsync();
            var (newBlocked, newError) = await reviews.SaveReviewAsync(
                clean.Id,
                null,
                userId,
                "Brand new on inactive.");
            Assert.Null(newBlocked);
            Assert.Equal("Product not found.", newError);
        }
        finally
        {
            if (reviewId.HasValue)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    DELETE FROM ProductReviewRevisions WHERE ReviewId = {reviewId.Value};
                    DELETE FROM ProductReviews WHERE Id = {reviewId.Value};
                    """);
            }

            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE ShopProducts SET IsActive = {(originalActive ? 1 : 0)} WHERE Id = {product.Id};
                UPDATE ShopProducts SET IsActive = CAST(1 AS bit) WHERE Id = {clean.Id};
                """);
        }
    }

    [Fact]
    public async Task Car_favorite_still_requires_active_listing()
    {
        await using var db = CreateDb();
        var config = new ConfigurationBuilder().Build();
        var marketplace = new MarketplaceService(db, config, new NotificationService(db));
        var userId = await db.IdentityUsers.AsNoTracking().Select(user => user.Id).FirstAsync();
        var inactive = await db.Cars.AsNoTracking()
            .FirstOrDefaultAsync(car => car.Status.ToLower() != "active");
        if (inactive == null)
        {
            return;
        }

        Assert.Null(await marketplace.AddFavoriteAsync(userId, inactive.Id));
    }

    private static FinalProjectDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new FinalProjectDbContext(options);
    }
}
