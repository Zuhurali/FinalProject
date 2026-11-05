using FinalProject.Commerce;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.Models.Entities;
using FinalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Services;

public sealed class NotificationService : INotificationService
{
    private readonly FinalProjectDbContext _context;

    public NotificationService(FinalProjectDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationListResponseDto> GetForUserAsync(
        string userId,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);
        var entities = await _context.UserNotifications
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
        var items = entities.Select(ToResponse).ToList();

        var unread = await GetUnreadCountAsync(userId, cancellationToken);
        return new NotificationListResponseDto(unread, items);
    }

    public Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default) =>
        _context.UserNotifications
            .AsNoTracking()
            .CountAsync(item => item.UserId == userId && !item.IsRead, cancellationToken);

    public async Task<NotificationUnreadCountsResponseDto> GetUnreadCountsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var unreadTypes = await _context.UserNotifications
            .AsNoTracking()
            .Where(item => item.UserId == userId && !item.IsRead)
            .Select(item => item.Type)
            .ToListAsync(cancellationToken);

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
        if (await ExistsAsync(receiverId, dedupKey, cancellationToken))
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

        return await TryInsertAsync(entity, cancellationToken);
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

        var favoriterIds = await _context.Favorites
            .AsNoTracking()
            .Where(favorite => favorite.CarId == carId)
            .Select(favorite => favorite.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

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
            if (await ExistsAsync(userId, baseDedup, cancellationToken))
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

            var response = await TryInsertAsync(entity, cancellationToken);
            if (response != null)
            {
                created.Add(response);
            }
        }

        return created;
    }

    public async Task<bool> MarkReadAsync(
        int notificationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _context.UserNotifications
            .SingleOrDefaultAsync(
                item => item.Id == notificationId && item.UserId == userId,
                cancellationToken);
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
        var unread = await _context.UserNotifications
            .Where(item => item.UserId == userId && !item.IsRead)
            .ToListAsync(cancellationToken);
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

    private Task<bool> ExistsAsync(
        string userId,
        string dedupKey,
        CancellationToken cancellationToken) =>
        _context.UserNotifications.AnyAsync(
            item => item.UserId == userId && item.DedupKey == dedupKey,
            cancellationToken);

    private async Task<NotificationResponseDto?> TryInsertAsync(
        UserNotification entity,
        CancellationToken cancellationToken)
    {
        _context.UserNotifications.Add(entity);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return ToResponse(entity);
        }
        catch (DbUpdateException)
        {
            _context.Entry(entity).State = EntityState.Detached;
            return null;
        }
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
