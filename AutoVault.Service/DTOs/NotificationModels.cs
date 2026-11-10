namespace FinalProject.DTOs;

public sealed record NotificationResponseDto(
    int Id,
    string UserId,
    string Type,
    string Title,
    string Body,
    string Url,
    bool IsRead,
    DateTime CreatedAt,
    int? RelatedConversationId,
    int? RelatedCarId);

public sealed record NotificationListResponseDto(
    int UnreadCount,
    IReadOnlyList<NotificationResponseDto> Items);

public sealed record NotificationUnreadCountsResponseDto(
    int Count,
    int Messages,
    int Favorites,
    int Listings,
    int Other);

public sealed record CarUpdateResultDto(
    CarResponseDto Car,
    IReadOnlyList<NotificationResponseDto> Notifications,
    IReadOnlyList<string>? RemovedImageUrls = null);
