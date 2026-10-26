using FinalProject.MVC.Models.Api;

namespace FinalProject.MVC.Services.Interfaces;

public interface ICarApiService
{
    Task<PagedCarsDto> GetCarsAsync(CarApiQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarApiDto>> GetAllCarsAsync(CarApiQuery? query = null, CancellationToken cancellationToken = default);

    Task<CarApiDto?> GetCarByIdAsync(int id, string? accessToken = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarSuggestionDto>> GetSuggestionsAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<CarLookupDto> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarApiDto>> GetMyCarsAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<CarApiDto> CreateCarAsync(SaveCarDto car, string accessToken, CancellationToken cancellationToken = default);

    Task<CarUpdateResultDto> UpdateCarAsync(int id, SaveCarDto car, string accessToken, CancellationToken cancellationToken = default);

    Task DeleteCarAsync(int id, string accessToken, CancellationToken cancellationToken = default);

    Task<CarApiDto> CreateCatalogCarAsync(SaveCarDto car, string accessToken, CancellationToken cancellationToken = default);

    Task RecordViewAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedRankedCarsDto> GetMostViewedAsync(string period, int page = 1, int pageSize = 9, CancellationToken cancellationToken = default);

    Task<PagedRankedCarsDto> GetMostFavoritedAsync(int page = 1, int pageSize = 9, CancellationToken cancellationToken = default);

    Task<PagedCarsDto> GetLatestArrivalsAsync(int page = 1, int pageSize = 9, CancellationToken cancellationToken = default);

    Task<CarDiscoveryStatisticsDto> GetDiscoveryStatisticsAsync(string accessToken, CancellationToken cancellationToken = default);
}
