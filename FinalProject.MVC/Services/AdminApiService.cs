using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Services;

public sealed class AdminApiService : IAdminApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public AdminApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<AdminSummaryDto> GetSummaryAsync(string accessToken, CancellationToken cancellationToken = default) =>
        GetAsync<AdminSummaryDto>("api/admin/summary", accessToken, cancellationToken);

    public Task<BrandApiDto> CreateBrandAsync(string name, string accessToken, CancellationToken cancellationToken = default) =>
        SendLookupAsync<BrandApiDto>(HttpMethod.Post, "api/admin/brands", name, accessToken, cancellationToken);

    public Task<BrandApiDto> UpdateBrandAsync(int id, string name, string accessToken, CancellationToken cancellationToken = default) =>
        SendLookupAsync<BrandApiDto>(HttpMethod.Put, $"api/admin/brands/{id}", name, accessToken, cancellationToken);

    public Task DeleteBrandAsync(int id, string accessToken, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/admin/brands/{id}", accessToken, cancellationToken);

    public Task<CategoryApiDto> CreateCategoryAsync(string name, string accessToken, CancellationToken cancellationToken = default) =>
        SendLookupAsync<CategoryApiDto>(HttpMethod.Post, "api/admin/categories", name, accessToken, cancellationToken);

    public Task<CategoryApiDto> UpdateCategoryAsync(int id, string name, string accessToken, CancellationToken cancellationToken = default) =>
        SendLookupAsync<CategoryApiDto>(HttpMethod.Put, $"api/admin/categories/{id}", name, accessToken, cancellationToken);

    public Task DeleteCategoryAsync(int id, string accessToken, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/admin/categories/{id}", accessToken, cancellationToken);

    private async Task<T> GetAsync<T>(string url, string accessToken, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, url, accessToken, null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new CarApiException("The API returned an unreadable response.");
    }

    private async Task<T> SendLookupAsync<T>(
        HttpMethod method,
        string url,
        string name,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            method,
            url,
            accessToken,
            JsonContent.Create(new SaveLookupDto { Name = name }),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new CarApiException("The API returned an unreadable lookup.");
    }

    private async Task DeleteAsync(string url, string accessToken, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Delete, url, accessToken, null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
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
            throw new CarApiException("The API could not be reached.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CarApiException("The API request timed out.", exception);
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = $"The API returned {(int)response.StatusCode}.";
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (document.RootElement.TryGetProperty("message", out var value)
                && !string.IsNullOrWhiteSpace(value.GetString()))
            {
                message = value.GetString()!;
            }
        }
        catch (JsonException)
        {
        }

        throw new CarApiException(message);
    }
}
