using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models.Api;

public sealed class SaveCarDto
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

    [Required]
    public string Status { get; set; } = "Active";
}

public sealed class CarLookupDto
{
    public List<BrandApiDto> Brands { get; set; } = new();

    public List<CategoryApiDto> Categories { get; set; } = new();
}

public sealed class FavoriteDto
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public CarApiDto Car { get; set; } = new();
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
}
