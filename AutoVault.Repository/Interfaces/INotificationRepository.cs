using FinalProject.Models.Entities;

namespace FinalProject.Repositories;

public interface INotificationRepository
{
    Task<IReadOnlyList<UserNotification>> GetForUserAsync(
        string userId,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetUnreadTypesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetDistinctFavoriterUserIdsAsync(
        int carId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByDedupAsync(
        string userId,
        string dedupKey,
        CancellationToken cancellationToken = default);

    Task<UserNotification?> TryInsertAsync(
        UserNotification entity,
        CancellationToken cancellationToken = default);

    Task<UserNotification?> FindForUserTrackedAsync(
        int notificationId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserNotification>> GetUnreadTrackedAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
