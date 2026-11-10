using FinalProject.Commerce;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.Models.Entities;
using FinalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Services;

public sealed class ProductReviewService : IProductReviewService
{
    private readonly FinalProjectDbContext _context;

    public ProductReviewService(FinalProjectDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProductReviewItemDto>?> GetReviewsAsync(
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

    public async Task<IReadOnlyList<ProductReviewItemDto>> GetAllForModerationAsync(
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProductReviews.AsNoTracking();
        return await Project(query, currentUserId: null, canModerate: true)
            .ToListAsync(cancellationToken);
    }

    public async Task<(ProductReviewItemDto? Review, string? Error)> SaveReviewAsync(
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

        if (!await _context.IdentityUsers.AsNoTracking().AnyAsync(user => user.Id == userId, cancellationToken))
        {
            return (null, "User not found.");
        }

        ProductReview review;
        if (reviewId.HasValue)
        {
            if (!await ProductExistsAsync(productId, cancellationToken))
            {
                return (null, "Product not found.");
            }

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

            RecordEdit(existing, normalized);
            review = existing;
        }
        else
        {
            if (!await ProductIsActiveAsync(productId, cancellationToken))
            {
                return (null, "Product not found.");
            }

            var existingForUser = await _context.ProductReviews
                .FirstOrDefaultAsync(item => item.ProductId == productId && item.UserId == userId, cancellationToken);
            if (existingForUser != null)
            {
                RecordEdit(existingForUser, normalized);
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
        CancellationToken cancellationToken = default)
    {
        var review = await _context.ProductReviews.FirstOrDefaultAsync(item => item.Id == reviewId, cancellationToken);
        if (review == null)
        {
            return (false, "Review not found.");
        }

        if (review.UserId != userId)
        {
            return (false, "You can delete only your own review.");
        }

        _context.ProductReviews.Remove(review);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<bool> DeleteReviewAsAdminAsync(
        int reviewId,
        CancellationToken cancellationToken = default)
    {
        var review = await _context.ProductReviews.FirstOrDefaultAsync(item => item.Id == reviewId, cancellationToken);
        if (review == null)
        {
            return false;
        }

        _context.ProductReviews.Remove(review);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetHiddenAsync(int reviewId, bool isHidden, CancellationToken cancellationToken = default)
    {
        var review = await _context.ProductReviews.FirstOrDefaultAsync(item => item.Id == reviewId, cancellationToken);
        if (review == null)
        {
            return false;
        }

        review.IsHidden = isHidden;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ProductReviewRevisionItemDto>?> GetHistoryAsync(
        int reviewId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _context.ProductReviews.AsNoTracking()
            .AnyAsync(review => review.Id == reviewId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        return await _context.ProductReviewRevisions.AsNoTracking()
            .Where(revision => revision.ReviewId == reviewId)
            .OrderByDescending(revision => revision.EditedAt)
            .ThenByDescending(revision => revision.Id)
            .Select(revision => new ProductReviewRevisionItemDto(
                revision.Id,
                revision.PreviousContent,
                revision.EditedAt))
            .ToListAsync(cancellationToken);
    }

    private void RecordEdit(ProductReview review, string normalized)
    {
        if (string.Equals(review.Content, normalized, StringComparison.Ordinal))
        {
            return;
        }

        _context.ProductReviewRevisions.Add(new ProductReviewRevision
        {
            ReviewId = review.Id,
            PreviousContent = review.Content,
            EditedAt = DateTime.UtcNow
        });
        review.Content = normalized;
        review.IsEdited = true;
        review.UpdatedAt = DateTime.UtcNow;
    }

    private IQueryable<ProductReviewItemDto> Project(IQueryable<ProductReview> query, string? currentUserId, bool canModerate)
    {
        // Soft-deleted authors keep their review rows and UserId in storage.
        // Public projection anonymizes the display name and omits UserId so
        // profile links cannot target a deleted Member.
        return from review in query
            join user in _context.IdentityUsers.AsNoTracking() on review.UserId equals user.Id into users
            from user in users.DefaultIfEmpty()
            orderby review.CreatedAt descending
            select new ProductReviewItemDto(
                review.Id,
                review.ProductId,
                user != null && user.IsDeleted ? string.Empty : review.UserId,
                user != null && user.IsDeleted
                    ? ReviewContentRules.DeletedAccountDisplayName
                    : (user.FirstName != null && user.FirstName != "") || (user.LastName != null && user.LastName != "")
                        ? ((user.FirstName ?? "") + " " + (user.LastName ?? "")).Trim()
                        : user.Email ?? user.UserName ?? "Customer",
                review.Content,
                review.IsHidden,
                review.CreatedAt,
                review.UpdatedAt ?? review.CreatedAt,
                review.UserId == currentUserId,
                review.IsEdited,
                canModerate && review.IsEdited);
    }

    private Task<bool> ProductExistsAsync(int productId, CancellationToken cancellationToken) =>
        _context.ShopProducts.AsNoTracking().AnyAsync(product => product.Id == productId, cancellationToken);

    private Task<bool> ProductIsActiveAsync(int productId, CancellationToken cancellationToken) =>
        _context.ShopProducts.AsNoTracking()
            .AnyAsync(product => product.Id == productId && product.IsActive, cancellationToken);
}
