namespace FinalProject.DTOs;

public sealed record ProductReviewItemDto(
    int Id,
    int ProductId,
    string UserId,
    string UserDisplayName,
    string Content,
    bool IsHidden,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool CanEdit,
    bool WasEdited,
    bool CanViewHistory);

public sealed record ProductReviewRevisionItemDto(
    int Id,
    string PreviousContent,
    DateTime EditedAt);

public sealed class SaveProductReviewRequestDto
{
    public string Content { get; set; } = string.Empty;
}
