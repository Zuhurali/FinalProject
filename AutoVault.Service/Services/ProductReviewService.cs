using FinalProject.Commerce;
using FinalProject.Repositories;
using FinalProject.DTOs;
using FinalProject.Models.Entities;
using FinalProject.Services.Interfaces;

namespace FinalProject.Services;

public sealed class ProductReviewService : IProductReviewService
{
    private readonly IProductReviewRepository _context;
    private readonly IProductRatingRepository _products;

    public ProductReviewService(IProductReviewRepository context, IProductRatingRepository products)
    {
        _context = context;
        _products = products;
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

        var rows = await _context.GetReviewsForProductAsync(productId, currentUserId, includeHidden, cancellationToken);
        return rows.Select(ToItemDto).ToList();
    }

    public async Task<IReadOnlyList<ProductReviewItemDto>> GetAllForModerationAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.GetAllReviewsForModerationAsync(cancellationToken);
        return rows.Select(ToItemDto).ToList();
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

        if (!await UserExistsAsync(userId, cancellationToken))
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

            var existing = await _context.FindReviewByIdAndProductTrackedAsync(
                reviewId.Value,
                productId,
                cancellationToken);
            if (existing == null)
            {
                return (null, "Review not found.");
            }

            if (existing.UserId != userId)
            {
                return (null, "You can edit only your own review.");
            }

            await _context.ApplyContentEditAsync(existing, normalized, cancellationToken);
            review = existing;
        }
        else
        {
            if (!await ProductIsActiveAsync(productId, cancellationToken))
            {
                return (null, "Product not found.");
            }

            var existingForUser = await _context.FindUserProductReviewTrackedAsync(productId, userId, cancellationToken);
            if (existingForUser != null)
            {
                await _context.ApplyContentEditAsync(existingForUser, normalized, cancellationToken);
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
                await _context.AddReviewAsync(review, cancellationToken);
                var saved = await _context.GetReviewListRowAsync(review.Id, userId, canModerate: false, cancellationToken);
                return (ToItemDto(saved), null);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        var row = await _context.GetReviewListRowAsync(review.Id, userId, canModerate: false, cancellationToken);
        return (ToItemDto(row), null);
    }

    public async Task<(bool Ok, string? Error)> DeleteReviewAsync(
        int reviewId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var review = await _context.FindReviewByIdTrackedAsync(reviewId, cancellationToken);
        if (review == null)
        {
            return (false, "Review not found.");
        }

        if (review.UserId != userId)
        {
            return (false, "You can delete only your own review.");
        }

        await _context.DeleteReviewAsync(review, cancellationToken);
        return (true, null);
    }

    public async Task<bool> DeleteReviewAsAdminAsync(
        int reviewId,
        CancellationToken cancellationToken = default)
    {
        var review = await _context.FindReviewByIdTrackedAsync(reviewId, cancellationToken);
        if (review == null)
        {
            return false;
        }

        await _context.DeleteReviewAsync(review, cancellationToken);
        return true;
    }

    public Task<bool> SetHiddenAsync(int reviewId, bool isHidden, CancellationToken cancellationToken = default) =>
        _context.SetReviewHiddenAsync(reviewId, isHidden, cancellationToken);

    public async Task<IReadOnlyList<ProductReviewRevisionItemDto>?> GetHistoryAsync(
        int reviewId,
        CancellationToken cancellationToken = default)
    {
        if (!await _context.ReviewExistsAsync(reviewId, cancellationToken))
        {
            return null;
        }

        var rows = await _context.GetRevisionHistoryAsync(reviewId, cancellationToken);
        return rows
            .Select(revision => new ProductReviewRevisionItemDto(
                revision.Id,
                revision.PreviousContent,
                revision.EditedAt))
            .ToList();
    }

    private Task<bool> ProductExistsAsync(int productId, CancellationToken cancellationToken) =>
        _products.ProductExistsAsync(productId, cancellationToken);

    private Task<bool> ProductIsActiveAsync(int productId, CancellationToken cancellationToken) =>
        _products.ProductIsActiveAsync(productId, cancellationToken);

    private Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken) =>
        _products.UserExistsAsync(userId, cancellationToken);

    private static ProductReviewItemDto ToItemDto(ProductReviewListRow row) =>
        new(
            row.Id,
            row.ProductId,
            row.UserId,
            row.UserDisplayName,
            row.Content,
            row.IsHidden,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsCurrentUser,
            row.IsEdited,
            row.ShowEditHistory);
}
