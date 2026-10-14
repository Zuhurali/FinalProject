using FinalProject.Commerce;
using FinalProject.Data;
using FinalProject.Models;
using FinalProject.Models.Entities;
using FinalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Services;

public sealed class ProductReviewService : IProductReviewService
{
    private readonly FinalProjectDbContext _context;
    private readonly HashSet<int> _productIds;

    public ProductReviewService(FinalProjectDbContext context, IConfiguration configuration)
    {
        _context = context;
        _productIds = configuration.GetSection("ShopCatalog:ProductIds").Get<int[]>()?.ToHashSet() ?? [];
    }

    public async Task<IReadOnlyList<ProductReviewItem>?> GetReviewsAsync(
        int productId,
        string? currentUserId,
        bool includeHidden,
        CancellationToken cancellationToken = default)
    {
        if (!await ProductExistsAsync(productId, cancellationToken))
        {
            return null;
        }

        var query = _context.ProductReviews.AsNoTracking().Where(review => review.ProductId == productId);
        if (!includeHidden)
        {
            query = query.Where(review => !review.IsHidden);
        }

        var reviews = await Project(query, currentUserId, canModerate: includeHidden)
            .ToListAsync(cancellationToken);
        return reviews;
    }

    public async Task<IReadOnlyList<ProductReviewItem>> GetAllForModerationAsync(
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProductReviews.AsNoTracking();
        return await Project(query, currentUserId: null, canModerate: true)
            .ToListAsync(cancellationToken);
    }

    public async Task<(ProductReviewItem? Review, string? Error)> SaveReviewAsync(
        int productId,
        int? reviewId,
        string userId,
        string? content,
        CancellationToken cancellationToken = default)
    {
        var error = ReviewContentRules.Normalize(content, out var normalized);
        if (error != null)
        {
            return (null, error);
        }

        if (!await ProductExistsAsync(productId, cancellationToken))
        {
            return (null, "Product not found.");
        }

        if (!await _context.IdentityUsers.AsNoTracking().AnyAsync(user => user.Id == userId, cancellationToken))
        {
            return (null, "User not found.");
        }

        ProductReview review;
        if (reviewId.HasValue)
        {
            var existing = await _context.ProductReviews
                .FirstOrDefaultAsync(item => item.Id == reviewId.Value && item.ProductId == productId, cancellationToken);
            if (existing == null)
            {
                return (null, "Review not found.");
            }

            if (existing.UserId != userId)
            {
                return (null, "You can edit only your own review.");
            }

            existing.Content = normalized;
            existing.UpdatedAt = DateTime.UtcNow;
            review = existing;
        }
        else
        {
            var existingForUser = await _context.ProductReviews
                .FirstOrDefaultAsync(item => item.ProductId == productId && item.UserId == userId, cancellationToken);
            if (existingForUser != null)
            {
                existingForUser.Content = normalized;
                existingForUser.UpdatedAt = DateTime.UtcNow;
                review = existingForUser;
            }
            else
            {
                review = new ProductReview
                {
                    ProductId = productId,
                    UserId = userId,
                    Content = normalized,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.ProductReviews.Add(review);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        var saved = await Project(
                _context.ProductReviews.AsNoTracking().Where(item => item.Id == review.Id),
                userId,
                canModerate: false)
            .FirstAsync(cancellationToken);
        return (saved, null);
    }

    public async Task<(bool Ok, string? Error)> DeleteReviewAsync(
        int reviewId,
        string userId,
        bool isModerator,
        CancellationToken cancellationToken = default)
    {
        var review = await _context.ProductReviews.FirstOrDefaultAsync(item => item.Id == reviewId, cancellationToken);
        if (review == null)
        {
            return (false, "Review not found.");
        }

        if (!isModerator && review.UserId != userId)
        {
            return (false, "You can delete only your own review.");
        }

        _context.ProductReviews.Remove(review);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<bool> SetHiddenAsync(int reviewId, bool isHidden, CancellationToken cancellationToken = default)
    {
        var review = await _context.ProductReviews.FirstOrDefaultAsync(item => item.Id == reviewId, cancellationToken);
        if (review == null)
        {
            return false;
        }

        review.IsHidden = isHidden;
        review.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<ProductReviewItem> Project(IQueryable<ProductReview> query, string? currentUserId, bool canModerate)
    {
        return from review in query
            join user in _context.IdentityUsers.AsNoTracking() on review.UserId equals user.Id into users
            from user in users.DefaultIfEmpty()
            orderby review.CreatedAt descending
            select new ProductReviewItem(
                review.Id,
                review.ProductId,
                review.UserId,
                user.Email ?? user.UserName ?? "Customer",
                review.Content,
                review.IsHidden,
                review.CreatedAt,
                review.UpdatedAt,
                canModerate || review.UserId == currentUserId);
    }

    private async Task<bool> ProductExistsAsync(int productId, CancellationToken cancellationToken)
    {
        if (_productIds.Contains(productId))
        {
            return true;
        }

        return await _context.ShopProducts.AsNoTracking().AnyAsync(product => product.Id == productId, cancellationToken);
    }
}
