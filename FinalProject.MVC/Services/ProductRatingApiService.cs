using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Services;

public sealed class ProductRatingApiService : IProductRatingApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public ProductRatingApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProductRatingSummaryDto?> GetRatingsAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/products/{productId}/ratings");
        return await GetRatingsAsync(request, cancellationToken);
    }

    public async Task<ProductRatingSummaryDto?> GetRatingsForAdminAsync(
        int productId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = Authorized(
            HttpMethod.Get,
            $"api/products/{productId}/ratings",
            accessToken);
        return await GetRatingsAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductRatingItemDto>> GetAllForAdminAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = Authorized(HttpMethod.Get, "api/products/ratings/admin", accessToken);
        using var response = await SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<ProductRatingItemDto>>(
                   JsonOptions,
                   cancellationToken)
               ?? [];
    }

    private async Task<ProductRatingSummaryDto?> GetRatingsAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ProductRatingSummaryDto>(
            JsonOptions,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ProductRatingSummaryItemDto>> GetSummariesAsync(
        IEnumerable<int> productIds,
        CancellationToken cancellationToken = default)
    {
        var query = string.Join(
            "&",
            productIds.Distinct().Select(id => $"productIds={id}"));
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/products/ratings?{query}");
        using var response = await SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<ProductRatingSummaryItemDto>>(
                   JsonOptions,
                   cancellationToken)
               ?? [];
    }

    public async Task<CurrentUserProductRatingDto?> GetMyRatingAsync(
        int productId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = Authorized(
            HttpMethod.Get,
            $"api/products/{productId}/ratings/my",
            accessToken);
        using var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<CurrentUserProductRatingDto>(
            JsonOptions,
            cancellationToken);
    }

    public async Task<CurrentUserProductRatingDto> SaveRatingAsync(
        int productId,
        int rating,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = Authorized(
            HttpMethod.Post,
            $"api/products/{productId}/ratings",
            accessToken);
        request.Content = JsonContent.Create(new { rating });

        using var response = await SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<CurrentUserProductRatingDto>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new CarApiException("The rating API returned an unreadable response.");
    }

    public async Task DeleteRatingAsync(
        int productId,
        int ratingId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = Authorized(
            HttpMethod.Delete,
            $"api/products/{productId}/ratings/{ratingId}",
            accessToken);
        using var response = await SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task DeleteMyRatingAsync(
        int productId,
        int ratingId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = Authorized(
            HttpMethod.Delete,
            $"api/products/{productId}/ratings/{ratingId}/my",
            accessToken);
        using var response = await SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static HttpRequestMessage Authorized(
        HttpMethod method,
        string url,
        string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            request.Dispose();
            throw new CarApiException("The rating service could not be reached.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            request.Dispose();
            throw new CarApiException("The rating service request timed out.", exception);
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

        var message = await ReadMessageAsync(response, cancellationToken);
        throw new CarApiException(
            string.IsNullOrWhiteSpace(message)
                ? $"The rating API returned {(int)response.StatusCode} ({response.ReasonPhrase})."
                : message);
    }

    private static async Task<string?> ReadMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));
            return document.RootElement.TryGetProperty("message", out var value)
                ? value.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
