namespace FinalProject.DTOs;

public sealed record NotificationResponse(
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

public sealed record NotificationListResponse(
    int UnreadCount,
    IReadOnlyList<NotificationResponse> Items);

public sealed record NotificationUnreadCountsResponse(
    int Count,
    int Messages,
    int Favorites,
    int Listings,
    int Other);

public sealed record CarUpdateResult(
    CarResponse Car,
    IReadOnlyList<NotificationResponse> Notifications);
