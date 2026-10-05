using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Services;

public class CarApiService : ICarApiService
{
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

        var page = await response.Content.ReadFromJsonAsync<PagedCarsDto>(cancellationToken);
        if (page?.Items == null)
        {
            throw new CarApiException("The car API returned an unreadable car list.");
        }

        return page;
    }

    public async Task<IReadOnlyList<CarApiDto>> GetAllCarsAsync(CarApiQuery? query = null, CancellationToken cancellationToken = default)
    {
        var firstQuery = Copy(query);
        firstQuery.Page = 1;
        firstQuery.PageSize = CarApiQuery.MaxPageSize;

        var firstPage = await GetCarsAsync(firstQuery, cancellationToken);
        if (firstPage.TotalPages <= 1)
        {
            return firstPage.Items;
        }

        var cars = new List<CarApiDto>(firstPage.Items);
        for (var page = 2; page <= firstPage.TotalPages; page++)
        {
            var nextQuery = Copy(query);
            nextQuery.Page = page;
            nextQuery.PageSize = CarApiQuery.MaxPageSize;
            var nextPage = await GetCarsAsync(nextQuery, cancellationToken);
            cars.AddRange(nextPage.Items);
        }

        return cars;
    }

    public async Task<CarApiDto?> GetCarByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync($"api/cars/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new CarApiException(FailureMessage(response));
        }

        var car = await response.Content.ReadFromJsonAsync<CarApiDto>(cancellationToken);
        if (car == null)
        {
            throw new CarApiException($"The car API returned an unreadable car for id {id}.");
        }

        return car;
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
