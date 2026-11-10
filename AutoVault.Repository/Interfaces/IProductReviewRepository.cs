using FinalProject.Models.Entities;

namespace FinalProject.Repositories;

public sealed record ProductReviewListRow(
    int Id,
    int ProductId,
    string UserId,
    string UserDisplayName,
    string Content,
    bool IsHidden,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsCurrentUser,
    bool IsEdited,
    bool ShowEditHistory);

public sealed record ProductReviewRevisionRow(
    int Id,
    string PreviousContent,
    DateTime EditedAt);

public interface IProductReviewRepository
{
    Task<IReadOnlyList<ProductReviewListRow>> GetReviewsForProductAsync(
        int productId,
        string? currentUserId,
        bool includeHidden,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductReviewListRow>> GetAllReviewsForModerationAsync(
        CancellationToken cancellationToken = default);

    Task<ProductReview?> FindReviewByIdTrackedAsync(
        int reviewId,
        CancellationToken cancellationToken = default);

    Task<ProductReview?> FindReviewByIdAndProductTrackedAsync(
        int reviewId,
        int productId,
        CancellationToken cancellationToken = default);

    Task<ProductReview?> FindUserProductReviewTrackedAsync(
        int productId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<ProductReviewListRow> GetReviewListRowAsync(
        int reviewId,
        string? currentUserId,
        bool canModerate,
        CancellationToken cancellationToken = default);

    Task AddReviewAsync(ProductReview review, CancellationToken cancellationToken = default);
    Task ApplyContentEditAsync(ProductReview review, string normalized, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task DeleteReviewAsync(ProductReview review, CancellationToken cancellationToken = default);
    Task<bool> SetReviewHiddenAsync(int reviewId, bool isHidden, CancellationToken cancellationToken = default);

    Task<bool> ReviewExistsAsync(int reviewId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductReviewRevisionRow>> GetRevisionHistoryAsync(
        int reviewId,
        CancellationToken cancellationToken = default);
}
