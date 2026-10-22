using FinalProject.DTOs;

namespace FinalProject.Services.Interfaces;

public interface INotificationService
{
    Task<NotificationListResponse> GetForUserAsync(
        string userId,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

    Task<NotificationUnreadCountsResponse> GetUnreadCountsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<NotificationResponse?> CreateMessageNotificationAsync(
        string receiverId,
        string senderDisplayName,
        int conversationId,
        int messageId,
        string preview,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationResponse>> CreatePriceDropNotificationsAsync(
        int carId,
        string carName,
        decimal previousPrice,
        decimal newPrice,
        CancellationToken cancellationToken = default);

    Task<bool> MarkReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default);

    Task<int> MarkAllReadAsync(string userId, CancellationToken cancellationToken = default);
}
