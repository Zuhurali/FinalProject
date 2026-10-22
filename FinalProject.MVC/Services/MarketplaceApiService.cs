using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Services;

public sealed class MarketplaceApiService : IMarketplaceApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public MarketplaceApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<AccountSummaryDto> GetSummaryAsync(
        string accessToken,
        CancellationToken cancellationToken = default) =>
        GetAsync<AccountSummaryDto>("api/account/summary", accessToken, cancellationToken);

    public async Task<IReadOnlyList<FavoriteDto>> GetFavoritesAsync(
        string accessToken,
        CancellationToken cancellationToken = default) =>
        await GetAsync<List<FavoriteDto>>("api/favorites", accessToken, cancellationToken);

    public async Task<IReadOnlyList<int>> GetFavoriteIdsAsync(
        string accessToken,
        CancellationToken cancellationToken = default) =>
        await GetAsync<List<int>>("api/favorites/ids", accessToken, cancellationToken);

    public async Task AddFavoriteAsync(
        int carId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Post,
            $"api/cars/{carId}/favorite",
            accessToken,
            null,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task RemoveFavoriteAsync(
        int carId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Delete,
            $"api/cars/{carId}/favorite",
            accessToken,
            null,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsAsync(
        string accessToken,
        CancellationToken cancellationToken = default) =>
        await GetAsync<List<ConversationSummaryDto>>(
            "api/conversations",
            accessToken,
            cancellationToken);

    public async Task<ConversationDetailsDto?> GetConversationAsync(
        int id,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            $"api/conversations/{id}",
            accessToken,
            null,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ConversationDetailsDto>(
            JsonOptions,
            cancellationToken);
    }

    public async Task<ConversationDetailsDto> StartConversationAsync(
        int carId,
        string? subject,
        string content,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Post,
            $"api/cars/{carId}/conversations",
            accessToken,
            JsonContent.Create(new { subject, content }),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ConversationDetailsDto>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new CarApiException("The marketplace API returned an unreadable conversation.");
    }

    public async Task<MessageDto> ReplyAsync(
        int conversationId,
        string content,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Post,
            $"api/conversations/{conversationId}/messages",
            accessToken,
            JsonContent.Create(new { content }),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<MessageDto>(JsonOptions, cancellationToken)
            ?? throw new CarApiException("The marketplace API returned an unreadable message.");
    }

    public async Task HideConversationAsync(
        int conversationId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Post,
            $"api/conversations/{conversationId}/hide",
            accessToken,
            null,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<T> GetAsync<T>(
        string url,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            url,
            accessToken,
            null,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new CarApiException("The marketplace API returned an unreadable response.");
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string url,
        string accessToken,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = content;
        try
        {
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new CarApiException("The marketplace API could not be reached.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CarApiException("The marketplace API request timed out.", exception);
        }
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string? message = null;
        try
        {
            using var document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));
            if (document.RootElement.TryGetProperty("message", out var value))
            {
                message = value.GetString();
            }
        }
        catch (JsonException)
        {
        }

        throw new CarApiException(
            string.IsNullOrWhiteSpace(message)
                ? $"The marketplace API returned {(int)response.StatusCode} ({response.ReasonPhrase})."
                : message);
    }
}
