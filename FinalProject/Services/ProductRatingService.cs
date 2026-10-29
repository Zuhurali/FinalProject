using FinalProject.Data;
using FinalProject.Models;
using FinalProject.Models.Entities;
using FinalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Services;

public sealed class ProductRatingService : IProductRatingService
{
    private readonly FinalProjectDbContext _context;

    public ProductRatingService(FinalProjectDbContext context)
    {
        _context = context;
    }

    public async Task<ProductRatingSummary?> GetProductRatingsAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        if (!await ProductExistsAsync(productId, cancellationToken))
        {
            return null;
        }

        var ratings = await (
            from rating in _context.ProductRatings.AsNoTracking()
            join user in _context.IdentityUsers.AsNoTracking()
                on rating.UserId equals user.Id
            where rating.ProductId == productId
            orderby rating.UpdatedAt descending
            select new ProductRatingItem(
                rating.Id,
                rating.ProductId,
                rating.UserId,
                user.Email ?? user.UserName ?? "User",
                rating.Rating,
                rating.CreatedAt,
                rating.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new ProductRatingSummary(
            productId,
            ratings.Count == 0 ? 0 : Math.Round(ratings.Average(item => item.Rating), 2),
            ratings.Count,
            ratings);
    }

    public async Task<IReadOnlyList<ProductRatingSummaryItem>> GetSummariesAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default)
    {
        var validIds = await ValidProductIdsAsync(productIds, cancellationToken);

        var aggregates = await _context.ProductRatings
            .AsNoTracking()
            .Where(rating => validIds.Contains(rating.ProductId))
            .GroupBy(rating => rating.ProductId)
            .Select(group => new ProductRatingSummaryItem(
                group.Key,
                Math.Round(group.Average(rating => rating.Rating), 2),
                group.Count()))
            .ToListAsync(cancellationToken);

        var byProduct = aggregates.ToDictionary(item => item.ProductId);
        return validIds
            .Select(productId => byProduct.GetValueOrDefault(productId)
                ?? new ProductRatingSummaryItem(productId, 0, 0))
            .ToList();
    }

    public async Task<IReadOnlyList<ProductRatingItem>> GetAllForAdminAsync(
        CancellationToken cancellationToken = default)
    {
        return await (
            from rating in _context.ProductRatings.AsNoTracking()
            join user in _context.IdentityUsers.AsNoTracking()
                on rating.UserId equals user.Id
            orderby rating.UpdatedAt descending
            select new ProductRatingItem(
                rating.Id,
                rating.ProductId,
                rating.UserId,
                user.Email ?? user.UserName ?? "User",
                rating.Rating,
                rating.CreatedAt,
                rating.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<CurrentUserProductRating?> GetUserRatingAsync(
        int productId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!await ProductExistsAsync(productId, cancellationToken) || !await UserExistsAsync(userId, cancellationToken))
        {
            return null;
        }

        return await _context.ProductRatings
            .AsNoTracking()
            .Where(rating => rating.ProductId == productId && rating.UserId == userId)
            .Select(rating => new CurrentUserProductRating(
                rating.Id,
                rating.ProductId,
                rating.Rating,
                rating.CreatedAt,
                rating.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CurrentUserProductRating?> SaveRatingAsync(
        int productId,
        string userId,
        int rating,
        CancellationToken cancellationToken = default)
    {
        if (!await ProductIsActiveAsync(productId, cancellationToken) ||
            rating is < 1 or > 5 ||
            !await UserExistsAsync(userId, cancellationToken))
        {
            return null;
        }

        var now = DateTime.UtcNow;

        // One active rating per user per product — reject if one already exists.
        var existing = await _context.ProductRatings
            .SingleOrDefaultAsync(
                item => item.ProductId == productId && item.UserId == userId,
                cancellationToken);

        if (existing != null)
        {
            // Signal "already rated" with a distinct sentinel value so callers can
            // distinguish "rating rejected" from "user/product not found".
            return new CurrentUserProductRating(
                existing.Id,
                existing.ProductId,
                existing.Rating,
                existing.CreatedAt,
                existing.UpdatedAt)
            { AlreadyExists = true };
        }

        var entity = new ProductRating
        {
            ProductId = productId,
            UserId = userId,
            Rating = rating,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.ProductRatings.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return new CurrentUserProductRating(
            entity.Id,
            entity.ProductId,
            entity.Rating,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    public async Task<bool> DeleteRatingAsync(
        int productId,
        int ratingId,
        CancellationToken cancellationToken = default)
    {
        if (!await ProductExistsAsync(productId, cancellationToken))
        {
            return false;
        }

        var rating = await _context.ProductRatings.SingleOrDefaultAsync(
            item => item.ProductId == productId && item.Id == ratingId,
            cancellationToken);
        if (rating == null)
        {
            return false;
        }

        _context.ProductRatings.Remove(rating);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(bool Found, bool Allowed)> DeleteRatingByUserAsync(
        int productId,
        int ratingId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!await ProductExistsAsync(productId, cancellationToken))
        {
            return (false, false);
        }

        var rating = await _context.ProductRatings.SingleOrDefaultAsync(
            item => item.ProductId == productId && item.Id == ratingId,
            cancellationToken);

        if (rating == null)
        {
            return (false, false);
        }

        if (rating.UserId != userId)
        {
            return (true, false);   // found but belongs to someone else
        }

        _context.ProductRatings.Remove(rating);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, true);
    }

    private Task<bool> ProductExistsAsync(int productId, CancellationToken cancellationToken) =>
        _context.ShopProducts.AsNoTracking()
            .AnyAsync(product => product.Id == productId, cancellationToken);

    private Task<bool> ProductIsActiveAsync(int productId, CancellationToken cancellationToken) =>
        _context.ShopProducts.AsNoTracking()
            .AnyAsync(product => product.Id == productId && product.IsActive, cancellationToken);

    private async Task<int[]> ValidProductIdsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken)
    {
        var distinct = productIds.Distinct().ToArray();
        var active = await _context.ShopProducts.AsNoTracking()
            .Where(product => distinct.Contains(product.Id) && product.IsActive)
            .Select(product => product.Id)
            .ToListAsync(cancellationToken);
        var known = active.ToHashSet();
        return distinct.Where(known.Contains).ToArray();
    }

    private Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken) =>
        !string.IsNullOrWhiteSpace(userId)
            ? _context.IdentityUsers.AsNoTracking().AnyAsync(user => user.Id == userId, cancellationToken)
            : Task.FromResult(false);
}
