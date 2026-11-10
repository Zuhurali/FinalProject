using FinalProject.Models.Entities;

namespace FinalProject.Repositories;

public sealed record ProductRatingWithUserRow(
    int Id,
    int ProductId,
    string UserId,
    string UserDisplayName,
    int Rating,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ProductRatingAggregateRow(
    int ProductId,
    double AverageRating,
    int TotalCount);

public sealed record CurrentUserProductRatingRow(
    int Id,
    int ProductId,
    int Rating,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public interface IProductRatingRepository
{
    Task<bool> ProductExistsAsync(int productId, CancellationToken cancellationToken = default);
    Task<bool> ProductIsActiveAsync(int productId, CancellationToken cancellationToken = default);
    Task<int[]> GetValidActiveProductIdsAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken = default);
    Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductRatingWithUserRow>> GetRatingsWithUsersForProductAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductRatingAggregateRow>> GetAggregatesForProductsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductRatingWithUserRow>> GetAllRatingsWithUsersAsync(
        CancellationToken cancellationToken = default);

    Task<CurrentUserProductRatingRow?> GetUserRatingAsync(
        int productId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<ProductRating?> FindUserRatingTrackedAsync(
        int productId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<ProductRating?> FindRatingTrackedAsync(
        int productId,
        int ratingId,
        CancellationToken cancellationToken = default);

    Task<ProductRating> AddRatingAsync(ProductRating rating, CancellationToken cancellationToken = default);
    Task DeleteRatingAsync(ProductRating rating, CancellationToken cancellationToken = default);
}
