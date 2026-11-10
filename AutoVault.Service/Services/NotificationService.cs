using FinalProject.Commerce;
using FinalProject.Repositories;
using FinalProject.DTOs;
using FinalProject.Models.Entities;
using FinalProject.Services.Interfaces;

namespace FinalProject.Services;

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _context;

    public NotificationService(INotificationRepository context)
    {
        _context = context;
    }

    public async Task<NotificationListResponseDto> GetForUserAsync(
        string userId,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);
        var entities = await _context.GetForUserAsync(userId, take, cancellationToken);
        var items = entities.Select(ToResponse).ToList();

        var unread = await _context.CountUnreadAsync(userId, cancellationToken);
        return new NotificationListResponseDto(unread, items);
    }

    public Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default) =>
        _context.CountUnreadAsync(userId, cancellationToken);

    public async Task<NotificationUnreadCountsResponseDto> GetUnreadCountsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var unreadTypes = await _context.GetUnreadTypesAsync(userId, cancellationToken);

        var messages = 0;
        var favorites = 0;
        var listings = 0;
        var other = 0;
        foreach (var type in unreadTypes)
        {
            switch (NotificationRules.MenuCategory(type))
            {
                case NotificationRules.MenuMessages:
                    messages++;
                    break;
                case NotificationRules.MenuFavorites:
                    favorites++;
                    break;
                case NotificationRules.MenuListings:
                    listings++;
                    break;
                default:
                    other++;
                    break;
            }
        }

        return new NotificationUnreadCountsResponseDto(
            unreadTypes.Count,
            messages,
            favorites,
            listings,
            other);
    }

    public async Task<NotificationResponseDto?> CreateMessageNotificationAsync(
        string receiverId,
        string senderDisplayName,
        int conversationId,
        int messageId,
        string preview,
        CancellationToken cancellationToken = default)
    {
        var dedupKey = NotificationRules.MessageDedupKey(messageId);
        if (await _context.ExistsByDedupAsync(receiverId, dedupKey, cancellationToken))
        {
            return null;
        }

        var entity = new UserNotification
        {
            UserId = receiverId,
            Type = NotificationRules.MessageType,
            Title = "New message",
            Body = NotificationRules.FormatMessageBody(senderDisplayName, preview),
            Url = $"/Account/Messages/{conversationId}",
            DedupKey = dedupKey,
            RelatedConversationId = conversationId,
            RelatedMessageId = messageId,
            CreatedAt = DateTime.UtcNow
        };

        var inserted = await _context.TryInsertAsync(entity, cancellationToken);
        return inserted is null ? null : ToResponse(inserted);
    }

    public async Task<IReadOnlyList<NotificationResponseDto>> CreatePriceDropNotificationsAsync(
        int carId,
        string carName,
        decimal previousPrice,
        decimal newPrice,
        CancellationToken cancellationToken = default)
    {
        if (!NotificationRules.IsPriceDrop(previousPrice, newPrice))
        {
            return Array.Empty<NotificationResponseDto>();
        }

        var favoriterIds = await _context.GetDistinctFavoriterUserIdsAsync(carId, cancellationToken);
        if (favoriterIds.Count == 0)
        {
            return Array.Empty<NotificationResponseDto>();
        }

        var body = NotificationRules.FormatPriceDropBody(carName, previousPrice, newPrice);
        var baseDedup = NotificationRules.PriceDropDedupKey(carId, previousPrice, newPrice);
        var created = new List<NotificationResponseDto>();
        var now = DateTime.UtcNow;

        foreach (var userId in favoriterIds)
        {
            if (await _context.ExistsByDedupAsync(userId, baseDedup, cancellationToken))
            {
                continue;
            }

            var entity = new UserNotification
            {
                UserId = userId,
                Type = NotificationRules.PriceDropType,
                Title = "Special offer",
                Body = body,
                Url = $"/Cars/Details/{carId}",
                DedupKey = baseDedup,
                RelatedCarId = carId,
                CreatedAt = now
            };

            var inserted = await _context.TryInsertAsync(entity, cancellationToken);
            if (inserted != null)
            {
                created.Add(ToResponse(inserted));
            }
        }

        return created;
    }

    public async Task<bool> MarkReadAsync(
        int notificationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _context.FindForUserTrackedAsync(notificationId, userId, cancellationToken);
        if (notification == null)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<int> MarkAllReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var unread = await _context.GetUnreadTrackedAsync(userId, cancellationToken);
        if (unread.Count == 0)
        {
            return 0;
        }

        foreach (var item in unread)
        {
            item.IsRead = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return unread.Count;
    }

    private static NotificationResponseDto ToResponse(UserNotification item) =>
        new(
            item.Id,
            item.UserId,
            item.Type,
            item.Title,
            item.Body,
            item.Url,
            item.IsRead,
            item.CreatedAt,
            item.RelatedConversationId,
            item.RelatedCarId);
}
