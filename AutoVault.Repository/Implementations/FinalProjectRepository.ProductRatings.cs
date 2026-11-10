using FinalProject.Models.Entities;
using FinalProject.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Repositories.Implementations;

public sealed partial class FinalProjectRepository
{
    public Task<bool> ProductExistsAsync(int productId, CancellationToken cancellationToken = default) =>
        _db.ShopProducts.AsNoTracking()
            .AnyAsync(product => product.Id == productId, cancellationToken);

    public Task<bool> ProductIsActiveAsync(int productId, CancellationToken cancellationToken = default) =>
        _db.ShopProducts.AsNoTracking()
            .AnyAsync(product => product.Id == productId && product.IsActive, cancellationToken);

    public async Task<int[]> GetValidActiveProductIdsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default)
    {
        var distinct = productIds.Distinct().ToArray();
        var active = await _db.ShopProducts.AsNoTracking()
            .Where(product => distinct.Contains(product.Id) && product.IsActive)
            .Select(product => product.Id)
            .ToListAsync(cancellationToken);
        var known = active.ToHashSet();
        return distinct.Where(known.Contains).ToArray();
    }

    public Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken = default) =>
        !string.IsNullOrWhiteSpace(userId)
            ? _db.IdentityUsers.AsNoTracking().AnyAsync(user => user.Id == userId, cancellationToken)
            : Task.FromResult(false);

    public async Task<IReadOnlyList<ProductRatingWithUserRow>> GetRatingsWithUsersForProductAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from rating in _db.ProductRatings.AsNoTracking()
            join user in _db.IdentityUsers.AsNoTracking()
                on rating.UserId equals user.Id
            where rating.ProductId == productId
            orderby rating.UpdatedAt descending
            select new ProductRatingWithUserRow(
                rating.Id,
                rating.ProductId,
                rating.UserId,
                user.Email ?? user.UserName ?? "User",
                rating.Rating,
                rating.CreatedAt,
                rating.UpdatedAt ?? rating.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductRatingAggregateRow>> GetAggregatesForProductsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default)
    {
        return await _db.ProductRatings
            .AsNoTracking()
            .Where(rating => productIds.Contains(rating.ProductId))
            .GroupBy(rating => rating.ProductId)
            .Select(group => new ProductRatingAggregateRow(
                group.Key,
                Math.Round(group.Average(rating => rating.Rating), 2),
                group.Count()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductRatingWithUserRow>> GetAllRatingsWithUsersAsync(
        CancellationToken cancellationToken = default)
    {
        return await (
            from rating in _db.ProductRatings.AsNoTracking()
            join user in _db.IdentityUsers.AsNoTracking()
                on rating.UserId equals user.Id
            orderby rating.UpdatedAt descending
            select new ProductRatingWithUserRow(
                rating.Id,
                rating.ProductId,
                rating.UserId,
                user.Email ?? user.UserName ?? "User",
                rating.Rating,
                rating.CreatedAt,
                rating.UpdatedAt ?? rating.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public Task<CurrentUserProductRatingRow?> GetUserRatingAsync(
        int productId,
        string userId,
        CancellationToken cancellationToken = default) =>
        _db.ProductRatings
            .AsNoTracking()
            .Where(rating => rating.ProductId == productId && rating.UserId == userId)
            .Select(rating => new CurrentUserProductRatingRow(
                rating.Id,
                rating.ProductId,
                rating.Rating,
                rating.CreatedAt,
                rating.UpdatedAt ?? rating.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<ProductRating?> FindUserRatingTrackedAsync(
        int productId,
        string userId,
        CancellationToken cancellationToken = default) =>
        _db.ProductRatings.SingleOrDefaultAsync(
            item => item.ProductId == productId && item.UserId == userId,
            cancellationToken);

    public Task<ProductRating?> FindRatingTrackedAsync(
        int productId,
        int ratingId,
        CancellationToken cancellationToken = default) =>
        _db.ProductRatings.SingleOrDefaultAsync(
            item => item.ProductId == productId && item.Id == ratingId,
            cancellationToken);

    public async Task<ProductRating> AddRatingAsync(ProductRating rating, CancellationToken cancellationToken = default)
    {
        _db.ProductRatings.Add(rating);
        await _db.SaveChangesAsync(cancellationToken);
        return rating;
    }

    public async Task DeleteRatingAsync(ProductRating rating, CancellationToken cancellationToken = default)
    {
        _db.ProductRatings.Remove(rating);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
