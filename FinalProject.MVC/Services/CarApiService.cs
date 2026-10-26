using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Services;

public class CarApiService : ICarApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public CarApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedCarsDto> GetCarsAsync(CarApiQuery query, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(BuildCarsUrl(query), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new CarApiException(FailureMessage(response));
        }

        var page = await response.Content.ReadFromJsonAsync<PagedCarsDto>(JsonOptions, cancellationToken);
        if (page?.Items == null)
        {
            throw new CarApiException("The car API returned an unreadable car list.");
        }

        return page;
    }

    public async Task<IReadOnlyList<CarApiDto>> GetAllCarsAsync(CarApiQuery? query = null, CancellationToken cancellationToken = default)
    {
        // Public bulk loads default to Active. Callers that need another status must set it explicitly.
        // Admin listing uses GetCarsAsync (not this method) so Draft/Sold remain visible there.
        var firstQuery = Copy(query);
        if (string.IsNullOrWhiteSpace(firstQuery.Status))
        {
            firstQuery.Status = CarPublicListing.ActiveStatus;
        }

        firstQuery.Page = 1;
        firstQuery.PageSize = CarApiQuery.MaxPageSize;

        var firstPage = await GetCarsAsync(firstQuery, cancellationToken);
        var cars = new List<CarApiDto>(firstPage.Items);
        var totalPages = firstPage.TotalPages;
        if (totalPages <= 1 && firstPage.TotalCount > cars.Count && firstQuery.PageSize > 0)
        {
            totalPages = (int)Math.Ceiling(firstPage.TotalCount / (double)firstQuery.PageSize);
        }

        for (var pageNumber = 2; pageNumber <= totalPages && cars.Count < firstPage.TotalCount; pageNumber++)
        {
            var nextQuery = Copy(firstQuery);
            nextQuery.Page = pageNumber;
            nextQuery.PageSize = CarApiQuery.MaxPageSize;
            var nextPage = await GetCarsAsync(nextQuery, cancellationToken);
            cars.AddRange(nextPage.Items);
        }

        return cars;
    }

    public async Task<IReadOnlyList<CarSuggestionDto>> GetSuggestionsAsync(string query, int limit, CancellationToken cancellationToken = default)
    {
        var url = $"api/cars/suggestions?query={Uri.EscapeDataString(query)}&limit={limit}";
        using var response = await SendAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new CarApiException(FailureMessage(response));
        }

        var suggestions = await response.Content.ReadFromJsonAsync<List<CarSuggestionDto>>(JsonOptions, cancellationToken);
        return suggestions ?? new List<CarSuggestionDto>();
    }

    public async Task RecordViewAsync(int id, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/cars/{id}/views");
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new CarApiException("The car API could not be reached.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CarApiException("The car API request timed out.", exception);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return;
            }

            await EnsureSuccessAsync(response, cancellationToken);
        }
    }

    public async Task<PagedRankedCarsDto> GetMostViewedAsync(
        string period,
        int page = 1,
        int pageSize = 9,
        CancellationToken cancellationToken = default)
    {
        var url =
            $"api/cars/explore/most-viewed?period={Uri.EscapeDataString(period)}&page={page}&pageSize={pageSize}";
        using var response = await SendAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PagedRankedCarsDto>(JsonOptions, cancellationToken)
            ?? new PagedRankedCarsDto { CurrentPage = page, PageSize = pageSize };
    }

    public async Task<PagedRankedCarsDto> GetMostFavoritedAsync(
        int page = 1,
        int pageSize = 9,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/cars/explore/most-favorited?page={page}&pageSize={pageSize}";
        using var response = await SendAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PagedRankedCarsDto>(JsonOptions, cancellationToken)
            ?? new PagedRankedCarsDto { CurrentPage = page, PageSize = pageSize };
    }

    public async Task<PagedCarsDto> GetLatestArrivalsAsync(
        int page = 1,
        int pageSize = 9,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/cars/explore/latest?page={page}&pageSize={pageSize}";
        using var response = await SendAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PagedCarsDto>(JsonOptions, cancellationToken)
            ?? new PagedCarsDto { CurrentPage = page, PageSize = pageSize };
    }

    public async Task<CarDiscoveryStatisticsDto> GetDiscoveryStatisticsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get, "api/cars/explore/statistics", accessToken, null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<CarDiscoveryStatisticsDto>(JsonOptions, cancellationToken)
            ?? new CarDiscoveryStatisticsDto();
    }

    public async Task<CarApiDto?> GetCarByIdAsync(
        int id,
        string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        // Authenticated callers (admin/owner) can load Draft/Sold; anonymous public calls cannot.
        using var response = string.IsNullOrWhiteSpace(accessToken)
            ? await SendAsync($"api/cars/{id}", cancellationToken)
            : await SendAuthorizedAsync(HttpMethod.Get, $"api/cars/{id}", accessToken, null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new CarApiException(FailureMessage(response));
        }

        var car = await response.Content.ReadFromJsonAsync<CarApiDto>(JsonOptions, cancellationToken);
        if (car == null)
        {
            throw new CarApiException($"The car API returned an unreadable car for id {id}.");
        }

        return car;
    }

    public async Task<CarLookupDto> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync("api/cars/lookups", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new CarApiException(FailureMessage(response));
        }

        return await response.Content.ReadFromJsonAsync<CarLookupDto>(JsonOptions, cancellationToken)
            ?? throw new CarApiException("The car API returned unreadable lookup data.");
    }

    public async Task<IReadOnlyList<CarApiDto>> GetMyCarsAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAuthorizedAsync(
            HttpMethod.Get,
            "api/cars/mine",
            accessToken,
            null,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<CarApiDto>>(
                   JsonOptions,
                   cancellationToken)
               ?? [];
    }

    public async Task<CarApiDto> CreateCarAsync(
        SaveCarDto car,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAuthorizedAsync(
            HttpMethod.Post,
            "api/cars",
            accessToken,
            JsonContent.Create(car),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<CarApiDto>(JsonOptions, cancellationToken)
            ?? throw new CarApiException("The car API returned an unreadable listing.");
    }

    public async Task<CarUpdateResultDto> UpdateCarAsync(
        int id,
        SaveCarDto car,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAuthorizedAsync(
            HttpMethod.Put,
            $"api/cars/{id}",
            accessToken,
            JsonContent.Create(car),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<CarUpdateResultDto>(JsonOptions, cancellationToken)
            ?? throw new CarApiException("The car API returned an unreadable listing update.");
    }

    public async Task DeleteCarAsync(
        int id,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAuthorizedAsync(
            HttpMethod.Delete,
            $"api/cars/{id}",
            accessToken,
            null,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<CarApiDto> CreateCatalogCarAsync(
        SaveCarDto car,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAuthorizedAsync(
            HttpMethod.Post,
            "api/cars/catalog",
            accessToken,
            JsonContent.Create(car),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<CarApiDto>(JsonOptions, cancellationToken)
            ?? throw new CarApiException("The car API returned an unreadable listing.");
    }

    private async Task<HttpResponseMessage> SendAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.GetAsync(relativeUrl, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new CarApiException("The car API could not be reached.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CarApiException("The car API request timed out.", exception);
        }
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpMethod method,
        string relativeUrl,
        string accessToken,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, relativeUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = content;
        try
        {
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new CarApiException("The car API could not be reached.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CarApiException("The car API request timed out.", exception);
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

        var message = FailureMessage(response);
        try
        {
            using var document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));
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

    private static CarApiQuery Copy(CarApiQuery? query)
    {
        query ??= new CarApiQuery();
        return new CarApiQuery
        {
            Search = query.Search,
            BrandId = query.BrandId,
            CategoryId = query.CategoryId,
            Model = query.Model,
            MinPrice = query.MinPrice,
            MaxPrice = query.MaxPrice,
            MinYear = query.MinYear,
            MaxYear = query.MaxYear,
            Fuel = query.Fuel,
            Transmission = query.Transmission,
            IsNew = query.IsNew,
            Status = query.Status,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    private static string BuildCarsUrl(CarApiQuery query)
    {
        var parameters = new List<string>
        {
            $"page={query.Page}",
            $"pageSize={query.PageSize}"
        };

        Add(parameters, "search", query.Search);
        Add(parameters, "model", query.Model);
        Add(parameters, "fuel", query.Fuel);
        Add(parameters, "transmission", query.Transmission);
        Add(parameters, "status", query.Status);

        if (query.BrandId is int brandId)
        {
            parameters.Add($"brandId={brandId}");
        }

        if (query.CategoryId is int categoryId)
        {
            parameters.Add($"categoryId={categoryId}");
        }

        if (query.MinPrice is decimal minPrice)
        {
            parameters.Add($"minPrice={minPrice.ToString(CultureInfo.InvariantCulture)}");
        }

        if (query.MaxPrice is decimal maxPrice)
        {
            parameters.Add($"maxPrice={maxPrice.ToString(CultureInfo.InvariantCulture)}");
        }

        if (query.MinYear is int minYear)
        {
            parameters.Add($"minYear={minYear}");
        }

        if (query.MaxYear is int maxYear)
        {
            parameters.Add($"maxYear={maxYear}");
        }

        if (query.IsNew is bool isNew)
        {
            parameters.Add($"isNew={(isNew ? "true" : "false")}");
        }

        return "api/cars?" + string.Join("&", parameters);
    }

    private static void Add(List<string> parameters, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parameters.Add($"{name}={Uri.EscapeDataString(value)}");
        }
    }

    private static string FailureMessage(HttpResponseMessage response)
    {
        return $"The car API returned {(int)response.StatusCode} ({response.ReasonPhrase}).";
    }
}
