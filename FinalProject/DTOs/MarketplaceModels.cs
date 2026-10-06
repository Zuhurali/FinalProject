using System.ComponentModel.DataAnnotations;

namespace FinalProject.DTOs;

public sealed class SaveCarRequest
{
    [Range(1, int.MaxValue)]
    public int BrandId { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Required, MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    [Range(1900, 2100)]
    public int Year { get; set; }

    [Range(0.01, 999999999)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int Mileage { get; set; }

    [Required, MaxLength(50)]
    public string FuelType { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Transmission { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Color { get; set; } = string.Empty;

    [Required, MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [Required, RegularExpression("^(Active|Sold|Draft)$")]
    public string Status { get; set; } = "Active";
}

public sealed record CarLookupResponse(
    IReadOnlyList<BrandResponse> Brands,
    IReadOnlyList<CategoryResponse> Categories);

public sealed record FavoriteResponse(int Id, DateTime CreatedAt, CarResponse Car);

public sealed class StartConversationRequest
{
    [MaxLength(200)]
    public string? Subject { get; set; }

    [Required, StringLength(4000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}

public sealed class ReplyMessageRequest
{
    [Required, StringLength(4000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}

public sealed record MessageResponse(
    int Id,
    string SenderId,
    string SenderDisplayName,
    string ReceiverId,
    string Content,
    bool IsRead,
    DateTime CreatedAt);

public sealed record ConversationSummaryResponse(
    int Id,
    int CarId,
    string CarName,
    string? CarImageUrl,
    string OtherUserDisplayName,
    string LastMessage,
    DateTime UpdatedAt,
    int UnreadCount);

public sealed record ConversationDetailsResponse(
    int Id,
    int CarId,
    string CarName,
    string? CarImageUrl,
    string BuyerId,
    string BuyerDisplayName,
    string SellerId,
    string SellerDisplayName,
    string? Subject,
    IReadOnlyList<MessageResponse> Messages);

public sealed record AccountActivityResponse(
    string Type,
    string Description,
    DateTime CreatedAt,
    string? Url);

public sealed record AccountSummaryResponse(
    int ActiveListings,
    int Favorites,
    int UnreadMessages,
    IReadOnlyList<AccountActivityResponse> RecentActivity);
