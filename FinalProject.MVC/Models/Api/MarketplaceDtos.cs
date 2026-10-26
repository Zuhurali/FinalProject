using System.ComponentModel.DataAnnotations;
using FinalProject.MVC.Commerce;

namespace FinalProject.MVC.Models.Api;

public sealed class SaveCarDto : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Select a make.")]
    public int BrandId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a category.")]
    public int CategoryId { get; set; }

    [Required, MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    [Range(1900, 2100, ErrorMessage = "Enter a model year between 1900 and 2100.")]
    public int Year { get; set; }

    [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "Enter a price greater than 0.")]
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

    public List<string> Features { get; set; } = new();

    [Required, MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter an image path or URL, or choose a local image file."), MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public bool GallerySpecified { get; set; }

    public int? PrimaryImageId { get; set; }

    public List<int> RemoveImageIds { get; set; } = new();

    public List<string> AdditionalImageUrls { get; set; } = new();

    [Required, RegularExpression("^(Active|Sold|Draft)$", ErrorMessage = "Select a listing status.")]
    public string Status { get; set; } = "Active";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in CarListingRules.ValidateYear(Year))
        {
            yield return result;
        }

        foreach (var result in CarListingRules.ValidateOptionalSpecs(DriveType, Features))
        {
            yield return result;
        }

        foreach (var result in CarListingRules.ValidateDetails(
            FuelType, Transmission, MileageUnit, Horsepower, Doors, Cylinders, Steering, Vin,
            AccidentHistory, CustomsStatus, ConditionSpecified, Condition))
        {
            yield return result;
        }

        foreach (var result in CarListingRules.ValidateColorAndEngine(Color, Engine, FuelType))
        {
            yield return result;
        }

        if (!string.IsNullOrWhiteSpace(ImageUrl) && !IsSafeImageReference(ImageUrl))
        {
            yield return new ValidationResult("The primary image path is not allowed.", [nameof(ImageUrl)]);
        }
    }

    private static bool IsSafeImageReference(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var text = url.Trim();
        if (text.Length > 500 || text.Contains("..", StringComparison.Ordinal) || text.Contains('\\'))
        {
            return false;
        }

        return text.StartsWith("~/", StringComparison.Ordinal)
            || text.StartsWith("/", StringComparison.Ordinal)
            || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class CarLookupDto
{
    public List<BrandApiDto> Brands { get; set; } = new();

    public List<CategoryApiDto> Categories { get; set; } = new();

    public List<BrandModelDto> Models { get; set; } = new();
}

public sealed class BrandModelDto
{
    public int BrandId { get; set; }

    public string Name { get; set; } = string.Empty;
}

public sealed class FavoriteDto
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public CarApiDto Car { get; set; } = new();
}

public sealed class ProductFavoriteDto
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public int ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;

    public string ImagePath { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public bool InStock { get; set; }
}

public sealed class AccountActivityDto
{
    public string Key { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string? Url { get; set; }
}

public sealed class AccountSummaryDto
{
    public int ActiveListings { get; set; }

    public int Favorites { get; set; }

    public int UnreadMessages { get; set; }

    public List<AccountActivityDto> RecentActivity { get; set; } = new();
}

public sealed class ConversationSummaryDto
{
    public int Id { get; set; }

    public int CarId { get; set; }

    public string CarName { get; set; } = string.Empty;

    public string? CarImageUrl { get; set; }

    public string OtherUserDisplayName { get; set; } = string.Empty;

    public string LastMessage { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }

    public int UnreadCount { get; set; }
}

public sealed class MessageDto
{
    public int Id { get; set; }

    public string SenderId { get; set; } = string.Empty;

    public string SenderDisplayName { get; set; } = string.Empty;

    public string ReceiverId { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public NotificationDto? Notification { get; set; }
}

public sealed class ConversationDetailsDto
{
    public int Id { get; set; }

    public int CarId { get; set; }

    public string CarName { get; set; } = string.Empty;

    public string? CarImageUrl { get; set; }

    public string BuyerId { get; set; } = string.Empty;

    public string BuyerDisplayName { get; set; } = string.Empty;

    public string SellerId { get; set; } = string.Empty;

    public string SellerDisplayName { get; set; } = string.Empty;

    public string? Subject { get; set; }

    public List<MessageDto> Messages { get; set; } = new();

    public NotificationDto? Notification { get; set; }
}

public sealed class NotificationDto
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? RelatedConversationId { get; set; }

    public int? RelatedCarId { get; set; }
}

public sealed class NotificationListDto
{
    public int UnreadCount { get; set; }

    public List<NotificationDto> Items { get; set; } = new();
}

public sealed class NotificationUnreadCountsDto
{
    public int Count { get; set; }

    public int Messages { get; set; }

    public int Favorites { get; set; }

    public int Listings { get; set; }

    public int Other { get; set; }
}

public sealed class CarUpdateResultDto
{
    public CarApiDto Car { get; set; } = null!;

    public List<NotificationDto> Notifications { get; set; } = new();

    public List<string> RemovedImageUrls { get; set; } = new();
}
