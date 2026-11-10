using FinalProject.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Repositories.Implementations;

public sealed partial class FinalProjectRepository
{
    public async Task<IReadOnlyList<UserNotification>> GetForUserAsync(
        string userId,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _db.UserNotifications
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountUnreadAsync(string userId, CancellationToken cancellationToken = default) =>
        _db.UserNotifications
            .AsNoTracking()
            .CountAsync(item => item.UserId == userId && !item.IsRead, cancellationToken);

    public async Task<IReadOnlyList<string>> GetUnreadTypesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.UserNotifications
            .AsNoTracking()
            .Where(item => item.UserId == userId && !item.IsRead)
            .Select(item => item.Type)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetDistinctFavoriterUserIdsAsync(
        int carId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Favorites
            .AsNoTracking()
            .Where(favorite => favorite.CarId == carId)
            .Select(favorite => favorite.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByDedupAsync(
        string userId,
        string dedupKey,
        CancellationToken cancellationToken = default) =>
        _db.UserNotifications.AnyAsync(
            item => item.UserId == userId && item.DedupKey == dedupKey,
            cancellationToken);

    public async Task<UserNotification?> TryInsertAsync(
        UserNotification entity,
        CancellationToken cancellationToken = default)
    {
        _db.UserNotifications.Add(entity);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return entity;
        }
        catch (DbUpdateException)
        {
            _db.Entry(entity).State = EntityState.Detached;
            return null;
        }
    }

    public Task<UserNotification?> FindForUserTrackedAsync(
        int notificationId,
        string userId,
        CancellationToken cancellationToken = default) =>
        _db.UserNotifications
            .SingleOrDefaultAsync(
                item => item.Id == notificationId && item.UserId == userId,
                cancellationToken);

    public async Task<IReadOnlyList<UserNotification>> GetUnreadTrackedAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.UserNotifications
            .Where(item => item.UserId == userId && !item.IsRead)
            .ToListAsync(cancellationToken);
    }

    Task INotificationRepository.SaveChangesAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);
}
