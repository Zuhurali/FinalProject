using FinalProject.DTOs;

namespace FinalProject.Services.Interfaces;

public interface ICarService
{
    Task<PagedResultDto<CarResponseDto>> QueryAsync(CarListQueryDto query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarSuggestionResponseDto>> GetSuggestionsAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<CarResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarResponseDto>> GetOwnedAsync(string ownerId, CancellationToken cancellationToken = default);

    Task<CarResponseDto?> CreateAsync(string ownerId, SaveCarRequestDto request, CancellationToken cancellationToken = default);

    Task<CarResponseDto?> CreateCatalogAsync(SaveCarRequestDto request, CancellationToken cancellationToken = default);

    Task<CarUpdateResultDto?> UpdateAsync(int id, SaveCarRequestDto request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<CarLookupResponseDto> GetLookupsAsync(CancellationToken cancellationToken = default);
}
