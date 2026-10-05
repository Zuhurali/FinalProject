using FinalProject.DTOs;

namespace FinalProject.Services.Interfaces;

public interface ICarService
{
    Task<PagedResult<CarResponse>> QueryAsync(CarListQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarSuggestionResponse>> GetSuggestionsAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<CarResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
