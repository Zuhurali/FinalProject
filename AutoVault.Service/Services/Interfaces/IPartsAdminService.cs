using FinalProject.DTOs;

namespace FinalProject.Services.Interfaces;

public interface IPartsAdminService
{
    Task<PartAdminListResponseDto> ListAsync(
        string? search,
        string? category,
        string? brand,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PartAdminItemDto?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<(PartAdminItemDto? Part, string? Error)> CreateAsync(SavePartRequestDto request, CancellationToken cancellationToken = default);

    Task<(PartAdminItemDto? Part, string? Error)> UpdateAsync(int id, SavePartRequestDto request, CancellationToken cancellationToken = default);

    Task<(bool Found, string Outcome, string Message)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
