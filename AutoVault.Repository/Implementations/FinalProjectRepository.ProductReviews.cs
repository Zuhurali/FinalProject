using FinalProject.Commerce;
using FinalProject.Models.Entities;
using FinalProject.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Repositories.Implementations;

public sealed partial class FinalProjectRepository
{
    public Task<IReadOnlyList<ProductReviewListRow>> GetReviewsForProductAsync(
        int productId,
        string? currentUserId,
        bool includeHidden,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ProductReviews.AsNoTracking().Where(review => review.ProductId == productId);
        if (!includeHidden)
        {
            query = query.Where(review => !review.IsHidden);
        }

        return ProjectReviews(query, currentUserId, includeHidden, cancellationToken);
    }

    public Task<IReadOnlyList<ProductReviewListRow>> GetAllReviewsForModerationAsync(
        CancellationToken cancellationToken = default) =>
        ProjectReviews(_db.ProductReviews.AsNoTracking(), currentUserId: null, canModerate: true, cancellationToken);

    public Task<ProductReview?> FindReviewByIdTrackedAsync(int reviewId, CancellationToken cancellationToken = default) =>
        _db.ProductReviews.FirstOrDefaultAsync(item => item.Id == reviewId, cancellationToken);

    public Task<ProductReview?> FindReviewByIdAndProductTrackedAsync(
        int reviewId,
        int productId,
        CancellationToken cancellationToken = default) =>
        _db.ProductReviews.FirstOrDefaultAsync(
            item => item.Id == reviewId && item.ProductId == productId,
            cancellationToken);

    public Task<ProductReview?> FindUserProductReviewTrackedAsync(
        int productId,
        string userId,
        CancellationToken cancellationToken = default) =>
        _db.ProductReviews.FirstOrDefaultAsync(
            item => item.ProductId == productId && item.UserId == userId,
            cancellationToken);

    public async Task<ProductReviewListRow> GetReviewListRowAsync(
        int reviewId,
        string? currentUserId,
        bool canModerate,
        CancellationToken cancellationToken = default)
    {
        var rows = await ProjectReviews(
            _db.ProductReviews.AsNoTracking().Where(item => item.Id == reviewId),
            currentUserId,
            canModerate,
            cancellationToken);
        return rows[0];
    }

    public async Task AddReviewAsync(ProductReview review, CancellationToken cancellationToken = default)
    {
        _db.ProductReviews.Add(review);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task ApplyContentEditAsync(ProductReview review, string normalized, CancellationToken cancellationToken = default)
    {
        if (string.Equals(review.Content, normalized, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        _db.ProductReviewRevisions.Add(new ProductReviewRevision
        {
            ReviewId = review.Id,
            PreviousContent = review.Content,
            EditedAt = DateTime.UtcNow
        });
        review.Content = normalized;
        review.IsEdited = true;
        review.UpdatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    Task IProductReviewRepository.SaveChangesAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);

    public async Task DeleteReviewAsync(ProductReview review, CancellationToken cancellationToken = default)
    {
        _db.ProductReviews.Remove(review);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SetReviewHiddenAsync(int reviewId, bool isHidden, CancellationToken cancellationToken = default)
    {
        var review = await _db.ProductReviews.FirstOrDefaultAsync(item => item.Id == reviewId, cancellationToken);
        if (review == null)
        {
            return false;
        }

        review.IsHidden = isHidden;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> ReviewExistsAsync(int reviewId, CancellationToken cancellationToken = default) =>
        _db.ProductReviews.AsNoTracking().AnyAsync(review => review.Id == reviewId, cancellationToken);

    public async Task<IReadOnlyList<ProductReviewRevisionRow>> GetRevisionHistoryAsync(
        int reviewId,
        CancellationToken cancellationToken = default)
    {
        return await _db.ProductReviewRevisions.AsNoTracking()
            .Where(revision => revision.ReviewId == reviewId)
            .OrderByDescending(revision => revision.EditedAt)
            .ThenByDescending(revision => revision.Id)
            .Select(revision => new ProductReviewRevisionRow(
                revision.Id,
                revision.PreviousContent,
                revision.EditedAt))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ProductReviewListRow>> ProjectReviews(
        IQueryable<ProductReview> query,
        string? currentUserId,
        bool canModerate,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from review in query
            join user in _db.IdentityUsers.AsNoTracking() on review.UserId equals user.Id into users
            from user in users.DefaultIfEmpty()
            orderby review.CreatedAt descending
            select new ProductReviewListRow(
                review.Id,
                review.ProductId,
                user != null && user.IsDeleted ? string.Empty : review.UserId,
                user != null && user.IsDeleted
                    ? ReviewContentRules.DeletedAccountDisplayName
                    : (user.FirstName != null && user.FirstName != "") || (user.LastName != null && user.LastName != "")
                        ? ((user.FirstName ?? "") + " " + (user.LastName ?? "")).Trim()
                        : user.Email ?? user.UserName ?? "Customer",
                review.Content,
                review.IsHidden,
                review.CreatedAt,
                review.UpdatedAt ?? review.CreatedAt,
                review.UserId == currentUserId,
                review.IsEdited,
                canModerate && review.IsEdited))
            .ToListAsync(cancellationToken);
        return rows;
    }
}
