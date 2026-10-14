using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Services;

public sealed class ProductReviewApiService : IProductReviewApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public ProductReviewApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<ProductReviewDto>> GetReviewsAsync(
        int productId,
        string? accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = accessToken == null
            ? new HttpRequestMessage(HttpMethod.Get, $"api/products/{productId}/reviews")
            : Authorized(HttpMethod.Get, $"api/products/{productId}/reviews", accessToken);
        using var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<ProductReviewDto>>(JsonOptions, cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<ProductReviewDto>> GetForModerationAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = Authorized(HttpMethod.Get, "api/products/reviews/moderation", accessToken);
        using var response = await SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<ProductReviewDto>>(JsonOptions, cancellationToken) ?? [];
    }

    public async Task<ProductReviewDto> SaveReviewAsync(
        int productId,
        int? reviewId,
        string content,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var url = reviewId.HasValue
            ? $"api/products/{productId}/reviews/{reviewId.Value}"
            : $"api/products/{productId}/reviews";
        using var request = Authorized(reviewId.HasValue ? HttpMethod.Put : HttpMethod.Post, url, accessToken);
        request.Content = JsonContent.Create(new { content });
        using var response = await SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ProductReviewDto>(JsonOptions, cancellationToken)
            ?? throw new CarApiException("The review API returned an unreadable response.");
    }

    public async Task DeleteReviewAsync(int reviewId, string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = Authorized(HttpMethod.Delete, $"api/products/reviews/{reviewId}", accessToken);
        using var response = await SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task SetHiddenAsync(
        int reviewId,
        bool hidden,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = Authorized(
            HttpMethod.Post,
            $"api/products/reviews/{reviewId}/visibility?hidden={hidden.ToString().ToLowerInvariant()}",
            accessToken);
        using var response = await SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string url, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            request.Dispose();
            throw new CarApiException("The review service could not be reached.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            request.Dispose();
            throw new CarApiException("The review service request timed out.", exception);
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string? message = null;
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (document.RootElement.TryGetProperty("message", out var value))
            {
                message = value.GetString();
            }
        }
        catch (JsonException)
        {
        }

        throw new CarApiException(string.IsNullOrWhiteSpace(message)
            ? $"The review API returned {(int)response.StatusCode}."
            : message);
    }
}
