using FinalProject.DTOs;

namespace FinalProject.Services.Interfaces;

public interface IProductRatingService
{
    Task<ProductRatingSummaryDto?> GetProductRatingsAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductRatingSummaryItemDto>> GetSummariesAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductRatingItemDto>> GetAllForAdminAsync(
        CancellationToken cancellationToken = default);

    Task<CurrentUserProductRatingDto?> GetUserRatingAsync(
        int productId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<CurrentUserProductRatingDto?> SaveRatingAsync(
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
