using FinalProject.MVC.Models.Api;

namespace FinalProject.MVC.Services.Interfaces;

public interface IProductRatingApiService
{
    Task<ProductRatingSummaryDto?> GetRatingsAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<ProductRatingSummaryDto?> GetRatingsForAdminAsync(
        int productId,
        string accessToken,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductRatingSummaryItemDto>> GetSummariesAsync(
        IEnumerable<int> productIds,
        CancellationToken cancellationToken = default);

    Task<CurrentUserProductRatingDto?> GetMyRatingAsync(
        int productId,
        string accessToken,
        CancellationToken cancellationToken = default);

    Task<CurrentUserProductRatingDto> SaveRatingAsync(
        int productId,
        int rating,
        string accessToken,
        CancellationToken cancellationToken = default);

    Task DeleteRatingAsync(
        int productId,
        int ratingId,
        string accessToken,
        CancellationToken cancellationToken = default);

    Task DeleteMyRatingAsync(
        int productId,
        int ratingId,
        string accessToken,
        CancellationToken cancellationToken = default);
}
