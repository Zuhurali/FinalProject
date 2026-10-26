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

    public Task<PartAdminListDto> GetPartsAsync(
        string? search,
        string? category,
        int page,
        string accessToken,
        CancellationToken cancellationToken = default,
        string? brand = null)
    {
        var query = new List<string> { $"page={page}" };
        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add("search=" + Uri.EscapeDataString(search));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query.Add("category=" + Uri.EscapeDataString(category));
        }

        if (!string.IsNullOrWhiteSpace(brand))
        {
            query.Add("brand=" + Uri.EscapeDataString(brand));
        }

        return GetAsync<PartAdminListDto>("api/admin/parts?" + string.Join("&", query), accessToken, cancellationToken);
    }

    public Task<PartAdminItemDto> GetPartAsync(int id, string accessToken, CancellationToken cancellationToken = default) =>
        GetAsync<PartAdminItemDto>($"api/admin/parts/{id}", accessToken, cancellationToken);

    public Task<PartAdminItemDto> CreatePartAsync(SavePartDto part, string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync<PartAdminItemDto>(HttpMethod.Post, "api/admin/parts", part, accessToken, cancellationToken);

    public Task<PartAdminItemDto> UpdatePartAsync(int id, SavePartDto part, string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync<PartAdminItemDto>(HttpMethod.Put, $"api/admin/parts/{id}", part, accessToken, cancellationToken);

    public async Task<PartDeleteDto> DeletePartAsync(int id, string accessToken, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Delete, $"api/admin/parts/{id}", accessToken, null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PartDeleteDto>(JsonOptions, cancellationToken)
            ?? new PartDeleteDto { Outcome = "deleted", Message = "The part was deleted." };
    }

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

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string url,
        SavePartDto part,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var response = await SendAsync(method, url, accessToken, JsonContent.Create(part), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new CarApiException("The API returned an unreadable part.");
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
            else if (document.RootElement.TryGetProperty("errors", out var errors)
                && errors.ValueKind == JsonValueKind.Object)
            {
                var details = new List<string>();
                foreach (var property in errors.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var item in property.Value.EnumerateArray())
                    {
                        var text = item.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            details.Add(text);
                        }
                    }
                }

                if (details.Count > 0)
                {
                    message = string.Join(" ", details.Distinct());
                }
            }
        }
        catch (JsonException)
        {
        }

        throw new CarApiException(message);
    }
}
