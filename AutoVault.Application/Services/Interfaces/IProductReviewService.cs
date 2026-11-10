using FinalProject.DTOs;

namespace FinalProject.Services.Interfaces;

public interface IProductReviewService
{
    Task<IReadOnlyList<ProductReviewItemDto>?> GetReviewsAsync(
        int productId,
        string? currentUserId,
        bool includeHidden,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductReviewItemDto>> GetAllForModerationAsync(
        CancellationToken cancellationToken = default);

    Task<(ProductReviewItemDto? Review, string? Error)> SaveReviewAsync(
        int productId,
        int? reviewId,
        string userId,
        string? content,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> DeleteReviewAsync(
        int reviewId,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a review for Admin/SuperAdmin moderation (no ownership check).
    /// Cascades revision history only; does not delete products or users.
    /// </summary>
    Task<bool> DeleteReviewAsAdminAsync(
        int reviewId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductReviewRevisionItemDto>?> GetHistoryAsync(
        int reviewId,
        CancellationToken cancellationToken = default);

    Task<bool> SetHiddenAsync(
        int reviewId,
        bool isHidden,
        CancellationToken cancellationToken = default);
}
