using FinalProject.MVC.Models.Api;

namespace FinalProject.MVC.Services.Interfaces;

public interface IProductReviewApiService
{
    Task<IReadOnlyList<ProductReviewDto>> GetReviewsAsync(
        int productId,
        string? accessToken,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductReviewDto>> GetForModerationAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    Task<ProductReviewDto> SaveReviewAsync(
        int productId,
        int? reviewId,
        string content,
        string accessToken,
        CancellationToken cancellationToken = default);

    Task DeleteReviewAsync(int reviewId, string accessToken, CancellationToken cancellationToken = default);

    Task DeleteReviewAsAdminAsync(int reviewId, string accessToken, CancellationToken cancellationToken = default);

    Task SetHiddenAsync(int reviewId, bool hidden, string accessToken, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductReviewRevisionDto>> GetHistoryAsync(
        int reviewId,
        string accessToken,
        CancellationToken cancellationToken = default);
}
