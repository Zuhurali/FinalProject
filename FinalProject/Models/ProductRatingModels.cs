using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models;

public sealed class SaveProductRatingRequest
{
    [Range(1, 5)]
    public int Rating { get; set; }
}

public sealed record ProductRatingItem(
    int Id,
    int ProductId,
    string UserId,
    string UserDisplayName,
    int Rating,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ProductRatingSummary(
    int ProductId,
    double AverageRating,
    int TotalCount,
    IReadOnlyList<ProductRatingItem> Ratings);

public sealed record ProductRatingSummaryItem(
    int ProductId,
    double AverageRating,
    int TotalCount);

public sealed record CurrentUserProductRating(
    int Id,
    int ProductId,
    int Rating,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    /// <summary>True when the user already has a rating for this product and the new submission was rejected.</summary>
    public bool AlreadyExists { get; init; }
}
