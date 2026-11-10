using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.DTOs;
using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Services;

public sealed class NotificationApiService : INotificationApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public NotificationApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<NotificationListDto> GetAsync(
        string accessToken,
        int take = 50,
        CancellationToken cancellationToken = default) =>
        GetAsync<NotificationListDto>($"api/notifications?take={take}", accessToken, cancellationToken);

    public Task<NotificationUnreadCountsDto> GetUnreadCountsAsync(
        string accessToken,
        CancellationToken cancellationToken = default) =>
        GetAsync<NotificationUnreadCountsDto>(
            "api/notifications/unread-count",
            accessToken,
            cancellationToken);

    public async Task<NotificationUnreadCountsDto> MarkReadAsync(
        int id,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Post,
            $"api/notifications/{id}/read",
            accessToken,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<NotificationUnreadCountsDto>(
                   JsonOptions,
                   cancellationToken)
               ?? new NotificationUnreadCountsDto();
    }

    public async Task<NotificationUnreadCountsDto> MarkAllReadAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Post,
            "api/notifications/read-all",
            accessToken,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<NotificationUnreadCountsDto>(
                   JsonOptions,
                   cancellationToken)
               ?? new NotificationUnreadCountsDto();
    }

    private async Task<T> GetAsync<T>(
        string url,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, url, accessToken, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
               ?? throw new CarApiException("The notification API returned an unreadable response.");
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string url,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new CarApiException(
            string.IsNullOrWhiteSpace(body)
                ? $"Notification API request failed ({(int)response.StatusCode})."
                : body);
    }
}
