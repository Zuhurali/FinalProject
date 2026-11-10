using System.ComponentModel.DataAnnotations;

namespace FinalProject.DTOs;

public sealed class SaveProductRatingRequestDto
{
    [Range(1, 5)]
    public int Rating { get; set; }
}

public sealed record ProductRatingItemDto(
    int Id,
    int ProductId,
    string UserId,
    string UserDisplayName,
    int Rating,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ProductRatingSummaryDto(
    int ProductId,
    double AverageRating,
    int TotalCount,
    IReadOnlyList<ProductRatingItemDto> Ratings);

public sealed record ProductRatingSummaryItemDto(
    int ProductId,
    double AverageRating,
    int TotalCount);

public sealed record CurrentUserProductRatingDto(
    int Id,
    int ProductId,
    int Rating,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    /// <summary>True when the user already has a rating for this product and the new submission was rejected.</summary>
    public bool AlreadyExists { get; init; }
}
