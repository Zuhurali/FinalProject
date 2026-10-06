using FinalProject.Data;
using FinalProject.Models;
using FinalProject.Models.Entities;
using FinalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Services;

public sealed class ProductRatingService : IProductRatingService
{
    private readonly FinalProjectDbContext _context;
    private readonly HashSet<int> _productIds;

    public ProductRatingService(
        FinalProjectDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _productIds = configuration
            .GetSection("ShopCatalog:ProductIds")
            .Get<int[]>()
            ?.ToHashSet() ?? [];
    }

    public async Task<ProductRatingSummary?> GetProductRatingsAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        if (!ProductExists(productId))
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
        var validIds = productIds
            .Where(ProductExists)
            .Distinct()
            .ToArray();

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

    public async Task<CurrentUserProductRating?> GetUserRatingAsync(
        int productId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!ProductExists(productId) || !await UserExistsAsync(userId, cancellationToken))
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
        if (!ProductExists(productId) ||
            rating is < 1 or > 5 ||
            !await UserExistsAsync(userId, cancellationToken))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var entity = await _context.ProductRatings
            .SingleOrDefaultAsync(
                item => item.ProductId == productId && item.UserId == userId,
                cancellationToken);

        if (entity == null)
        {
            entity = new ProductRating
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.ProductRatings.Add(entity);
        }
        else
        {
            entity.Rating = rating;
            entity.UpdatedAt = now;
        }

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
        if (!ProductExists(productId))
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

    private bool ProductExists(int productId) => _productIds.Contains(productId);

    private Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken) =>
        !string.IsNullOrWhiteSpace(userId)
            ? _context.IdentityUsers.AsNoTracking().AnyAsync(user => user.Id == userId, cancellationToken)
            : Task.FromResult(false);
}
