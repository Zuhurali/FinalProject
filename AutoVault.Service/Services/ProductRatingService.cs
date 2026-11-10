using FinalProject.Repositories;
using FinalProject.DTOs;
using FinalProject.Models.Entities;
using FinalProject.Services.Interfaces;

namespace FinalProject.Services;

public sealed class ProductRatingService : IProductRatingService
{
    private readonly IProductRatingRepository _context;

    public ProductRatingService(IProductRatingRepository context)
    {
        _context = context;
    }

    public async Task<ProductRatingSummaryDto?> GetProductRatingsAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        if (!await _context.ProductExistsAsync(productId, cancellationToken))
        {
            return null;
        }

        var rows = await _context.GetRatingsWithUsersForProductAsync(productId, cancellationToken);
        var ratings = rows.Select(ToItemDto).ToList();

        return new ProductRatingSummaryDto(
            productId,
            ratings.Count == 0 ? 0 : Math.Round(ratings.Average(item => item.Rating), 2),
            ratings.Count,
            ratings);
    }

    public async Task<IReadOnlyList<ProductRatingSummaryItemDto>> GetSummariesAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default)
    {
        var validIds = await _context.GetValidActiveProductIdsAsync(productIds, cancellationToken);

        var aggregates = await _context.GetAggregatesForProductsAsync(validIds, cancellationToken);
        var byProduct = aggregates.ToDictionary(
            item => item.ProductId,
            item => new ProductRatingSummaryItemDto(item.ProductId, item.AverageRating, item.TotalCount));

        return validIds
            .Select(productId => byProduct.GetValueOrDefault(productId)
                ?? new ProductRatingSummaryItemDto(productId, 0, 0))
            .ToList();
    }

    public async Task<IReadOnlyList<ProductRatingItemDto>> GetAllForAdminAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.GetAllRatingsWithUsersAsync(cancellationToken);
        return rows.Select(ToItemDto).ToList();
    }

    public async Task<CurrentUserProductRatingDto?> GetUserRatingAsync(
        int productId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!await _context.ProductExistsAsync(productId, cancellationToken)
            || !await _context.UserExistsAsync(userId, cancellationToken))
        {
            return null;
        }

        var row = await _context.GetUserRatingAsync(productId, userId, cancellationToken);
        return row is null
            ? null
            : new CurrentUserProductRatingDto(row.Id, row.ProductId, row.Rating, row.CreatedAt, row.UpdatedAt);
    }

    public async Task<CurrentUserProductRatingDto?> SaveRatingAsync(
        int productId,
        string userId,
        int rating,
        CancellationToken cancellationToken = default)
    {
        if (!await _context.ProductIsActiveAsync(productId, cancellationToken) ||
            rating is < 1 or > 5 ||
            !await _context.UserExistsAsync(userId, cancellationToken))
        {
            return null;
        }

        var now = DateTime.UtcNow;

        var existing = await _context.FindUserRatingTrackedAsync(productId, userId, cancellationToken);
        if (existing != null)
        {
            return new CurrentUserProductRatingDto(
                existing.Id,
                existing.ProductId,
                existing.Rating,
                existing.CreatedAt,
                existing.UpdatedAt ?? existing.CreatedAt)
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
        await _context.AddRatingAsync(entity, cancellationToken);

        return new CurrentUserProductRatingDto(
            entity.Id,
            entity.ProductId,
            entity.Rating,
            entity.CreatedAt,
            entity.UpdatedAt ?? entity.CreatedAt);
    }

    public async Task<bool> DeleteRatingAsync(
        int productId,
        int ratingId,
        CancellationToken cancellationToken = default)
    {
        if (!await _context.ProductExistsAsync(productId, cancellationToken))
        {
            return false;
        }

        var rating = await _context.FindRatingTrackedAsync(productId, ratingId, cancellationToken);
        if (rating == null)
        {
            return false;
        }

        await _context.DeleteRatingAsync(rating, cancellationToken);
        return true;
    }

    public async Task<(bool Found, bool Allowed)> DeleteRatingByUserAsync(
        int productId,
        int ratingId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!await _context.ProductExistsAsync(productId, cancellationToken))
        {
            return (false, false);
        }

        var rating = await _context.FindRatingTrackedAsync(productId, ratingId, cancellationToken);
        if (rating == null)
        {
            return (false, false);
        }

        if (rating.UserId != userId)
        {
            return (true, false);
        }

        await _context.DeleteRatingAsync(rating, cancellationToken);
        return (true, true);
    }

    private static ProductRatingItemDto ToItemDto(ProductRatingWithUserRow row) =>
        new(row.Id, row.ProductId, row.UserId, row.UserDisplayName, row.Rating, row.CreatedAt, row.UpdatedAt);
}
