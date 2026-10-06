using FinalProject.DTOs;

namespace FinalProject.Services.Interfaces;

public interface ICarService
{
    Task<PagedResult<CarResponse>> QueryAsync(CarListQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarSuggestionResponse>> GetSuggestionsAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<CarResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarResponse>> GetOwnedAsync(string ownerId, CancellationToken cancellationToken = default);

    Task<CarResponse?> CreateAsync(string ownerId, SaveCarRequest request, CancellationToken cancellationToken = default);

    Task<CarResponse?> UpdateAsync(int id, SaveCarRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<CarLookupResponse> GetLookupsAsync(CancellationToken cancellationToken = default);
}
