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

    public async Task<IReadOnlyList<CarApiDto>> GetCarsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync("api/cars", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new CarApiException(FailureMessage(response));
        }

        var cars = await response.Content.ReadFromJsonAsync<List<CarApiDto>>(cancellationToken);
        if (cars == null)
        {
            throw new CarApiException("The car API returned an unreadable car list.");
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

    private static string FailureMessage(HttpResponseMessage response)
    {
        return $"The car API returned {(int)response.StatusCode} ({response.ReasonPhrase}).";
    }
}
