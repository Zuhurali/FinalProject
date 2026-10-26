using System.ComponentModel.DataAnnotations;
using FinalProject.Commerce;

namespace FinalProject.DTOs;

public sealed class SaveCarRequest : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int BrandId { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Required, MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    [Range(1900, 2100, ErrorMessage = "Enter a model year between 1900 and 2100.")]
    public int Year { get; set; }

    [Range(0.01, 999999999, ErrorMessage = "Enter a price greater than 0.")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Enter mileage in miles. Use 0 only when the car has no miles.")]
    public int Mileage { get; set; }

    [Required, MaxLength(50)]
    public string FuelType { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Transmission { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Color { get; set; } = string.Empty;

    [Range(1, 12, ErrorMessage = "Enter seating capacity between 1 and 12.")]
    public int? Seats { get; set; }

    [MaxLength(20)]
    public string? DriveType { get; set; }

    [MaxLength(80, ErrorMessage = "Engine details must be 80 characters or fewer.")]
    public string? Engine { get; set; }

    [MaxLength(8)]
    public string? MileageUnit { get; set; }

    [Range(1, 2000)]
    public int? Horsepower { get; set; }

    [Range(2, 5)]
    public int? Doors { get; set; }

    [Range(1, 16)]
    public int? Cylinders { get; set; }

    [MaxLength(8)]
    public string? Steering { get; set; }

    [MaxLength(17)]
    public string? Vin { get; set; }

    [MaxLength(40)]
    public string? AccidentHistory { get; set; }

    [MaxLength(400)]
    public string? RepaintedParts { get; set; }

    [MaxLength(400)]
    public string? ReplacedParts { get; set; }

    [MaxLength(40)]
    public string? CustomsStatus { get; set; }

    [MaxLength(80)]
    public string? MarketOrigin { get; set; }

    public bool ConditionSpecified { get; set; }

    [MaxLength(10)]
    public string? Condition { get; set; }

    public bool FeaturesSpecified { get; set; }

    public List<string>? Features { get; set; }

    [Required, MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public bool GallerySpecified { get; set; }

    public int? PrimaryImageId { get; set; }

    public List<int> RemoveImageIds { get; set; } = new();

    public List<string> AdditionalImageUrls { get; set; } = new();

    [Required, RegularExpression("^(Active|Sold|Draft)$")]
    public string Status { get; set; } = "Active";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in Commerce.CarListingRules.ValidateYear(Year))
        {
            yield return result;
        }

        foreach (var result in Commerce.CarListingRules.ValidateOptionalSpecs(DriveType, Features))
        {
            yield return result;
        }

        foreach (var result in Commerce.CarListingRules.ValidateDetails(
            FuelType, Transmission, MileageUnit, Horsepower, Doors, Cylinders, Steering, Vin,
            AccidentHistory, CustomsStatus, ConditionSpecified, Condition))
        {
            yield return result;
        }

        foreach (var result in Commerce.CarListingRules.ValidateColorAndEngine(Color, Engine, FuelType))
        {
            yield return result;
        }

        if (!Commerce.CarGalleryRules.IsSafeReference(ImageUrl))
        {
            yield return new ValidationResult("The primary image path is not allowed.", [nameof(ImageUrl)]);
        }
    }
}

public sealed record CarLookupResponse(
    IReadOnlyList<BrandResponse> Brands,
    IReadOnlyList<CategoryResponse> Categories,
    IReadOnlyList<BrandModelResponse>? Models = null);

public sealed record BrandModelResponse(int BrandId, string Name);

public sealed record FavoriteResponse(int Id, DateTime CreatedAt, CarResponse Car);

public sealed record ProductFavoriteResponse(
    int Id,
    DateTime CreatedAt,
    int ProductId,
    string Name,
    decimal Price,
    string Category,
    string Brand,
    string ImagePath,
    int Quantity,
    bool InStock);

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
    DateTime CreatedAt,
    NotificationResponse? Notification = null);

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
    IReadOnlyList<MessageResponse> Messages,
    NotificationResponse? Notification = null);

public sealed record AccountActivityResponse(
    string Key,
    string Type,
    string Description,
    DateTime CreatedAt,
    string? Url);

public sealed record AccountSummaryResponse(
    int ActiveListings,
    int Favorites,
    int UnreadMessages,
    IReadOnlyList<AccountActivityResponse> RecentActivity);
