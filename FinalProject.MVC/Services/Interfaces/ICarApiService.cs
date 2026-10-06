using FinalProject.MVC.Models.Api;

namespace FinalProject.MVC.Services.Interfaces;

public interface ICarApiService
{
    Task<PagedCarsDto> GetCarsAsync(CarApiQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarApiDto>> GetAllCarsAsync(CarApiQuery? query = null, CancellationToken cancellationToken = default);

    Task<CarApiDto?> GetCarByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarSuggestionDto>> GetSuggestionsAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<CarLookupDto> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarApiDto>> GetMyCarsAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<CarApiDto> CreateCarAsync(SaveCarDto car, string accessToken, CancellationToken cancellationToken = default);

    Task<CarApiDto> UpdateCarAsync(int id, SaveCarDto car, string accessToken, CancellationToken cancellationToken = default);

    Task DeleteCarAsync(int id, string accessToken, CancellationToken cancellationToken = default);

    Task<CarApiDto> CreateCatalogCarAsync(SaveCarDto car, string accessToken, CancellationToken cancellationToken = default);
}
