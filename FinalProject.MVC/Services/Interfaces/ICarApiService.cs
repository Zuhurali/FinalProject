using FinalProject.MVC.Models.Api;

namespace FinalProject.MVC.Services.Interfaces;

public interface ICarApiService
{
    Task<PagedCarsDto> GetCarsAsync(CarApiQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarApiDto>> GetAllCarsAsync(CarApiQuery? query = null, CancellationToken cancellationToken = default);

    Task<CarApiDto?> GetCarByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarSuggestionDto>> GetSuggestionsAsync(string query, int limit, CancellationToken cancellationToken = default);
}
