using FinalProject.Models;

namespace FinalProject.Services.Interfaces;

public interface IProductReviewService
{
    Task<IReadOnlyList<ProductReviewItem>?> GetReviewsAsync(
        int productId,
        string? currentUserId,
        bool includeHidden,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductReviewItem>> GetAllForModerationAsync(
        CancellationToken cancellationToken = default);

    Task<(ProductReviewItem? Review, string? Error)> SaveReviewAsync(
        int productId,
        int? reviewId,
        string userId,
        string? content,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> DeleteReviewAsync(
        int reviewId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductReviewRevisionItem>?> GetHistoryAsync(
        int reviewId,
        CancellationToken cancellationToken = default);

    Task<bool> SetHiddenAsync(
        int reviewId,
        bool isHidden,
        CancellationToken cancellationToken = default);
}
