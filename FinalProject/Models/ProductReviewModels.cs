namespace FinalProject.Models;

public sealed record ProductReviewItem(
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

public sealed record ProductReviewRevisionItem(
    int Id,
    string PreviousContent,
    DateTime EditedAt);

public sealed class SaveProductReviewRequest
{
    public string Content { get; set; } = string.Empty;
}
