using FinalProject.Models;

namespace FinalProject.Services.Interfaces;

public interface IProductRatingService
{
    Task<ProductRatingSummary?> GetProductRatingsAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductRatingSummaryItem>> GetSummariesAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductRatingItem>> GetAllForAdminAsync(
        CancellationToken cancellationToken = default);

    Task<CurrentUserProductRating?> GetUserRatingAsync(
        int productId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<CurrentUserProductRating?> SaveRatingAsync(
        int productId,
        string userId,
        int rating,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteRatingAsync(
        int productId,
        int ratingId,
        CancellationToken cancellationToken = default);

    /// <summary>Allows the rating's owner to delete their own rating.</summary>
    Task<(bool Found, bool Allowed)> DeleteRatingByUserAsync(
        int productId,
        int ratingId,
        string userId,
        CancellationToken cancellationToken = default);
}
