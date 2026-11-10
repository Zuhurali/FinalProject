using FinalProject.MVC.DTOs;

namespace FinalProject.MVC.Services.Interfaces;

public interface INotificationApiService
{
    Task<NotificationListDto> GetAsync(string accessToken, int take = 50, CancellationToken cancellationToken = default);

    Task<NotificationUnreadCountsDto> GetUnreadCountsAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    Task<NotificationUnreadCountsDto> MarkReadAsync(
        int id,
        string accessToken,
        CancellationToken cancellationToken = default);

    Task<NotificationUnreadCountsDto> MarkAllReadAsync(
        string accessToken,
        CancellationToken cancellationToken = default);
}
