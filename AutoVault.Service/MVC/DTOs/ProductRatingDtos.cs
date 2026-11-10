namespace FinalProject.MVC.DTOs;

public sealed class ProductRatingItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class ProductRatingSummaryDto
{
    public int ProductId { get; set; }
    public double AverageRating { get; set; }
    public int TotalCount { get; set; }
    public List<ProductRatingItemDto> Ratings { get; set; } = [];
}

public sealed class ProductRatingSummaryItemDto
{
    public int ProductId { get; set; }
    public double AverageRating { get; set; }
    public int TotalCount { get; set; }
}

public sealed class CurrentUserProductRatingDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Rating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
