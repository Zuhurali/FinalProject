using FinalProject.DTOs;

namespace FinalProject.Services.Interfaces;

public interface IPartsAdminService
{
    Task<PartAdminListResponse> ListAsync(
        string? search,
        string? category,
        string? brand,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PartAdminItem?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<(PartAdminItem? Part, string? Error)> CreateAsync(SavePartRequest request, CancellationToken cancellationToken = default);

    Task<(PartAdminItem? Part, string? Error)> UpdateAsync(int id, SavePartRequest request, CancellationToken cancellationToken = default);

    Task<(bool Found, string Outcome, string Message)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
