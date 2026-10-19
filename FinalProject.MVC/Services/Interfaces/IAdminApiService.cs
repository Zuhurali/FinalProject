using FinalProject.MVC.Models.Api;

namespace FinalProject.MVC.Services.Interfaces;

public interface IAdminApiService
{
    Task<AdminSummaryDto> GetSummaryAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<BrandApiDto> CreateBrandAsync(string name, string accessToken, CancellationToken cancellationToken = default);

    Task<BrandApiDto> UpdateBrandAsync(int id, string name, string accessToken, CancellationToken cancellationToken = default);

    Task DeleteBrandAsync(int id, string accessToken, CancellationToken cancellationToken = default);

    Task<CategoryApiDto> CreateCategoryAsync(string name, string accessToken, CancellationToken cancellationToken = default);

    Task<CategoryApiDto> UpdateCategoryAsync(int id, string name, string accessToken, CancellationToken cancellationToken = default);

    Task DeleteCategoryAsync(int id, string accessToken, CancellationToken cancellationToken = default);

    Task<PartAdminListDto> GetPartsAsync(string? search, string? category, int page, string accessToken, CancellationToken cancellationToken = default);

    Task<PartAdminItemDto> GetPartAsync(int id, string accessToken, CancellationToken cancellationToken = default);

    Task<PartAdminItemDto> CreatePartAsync(SavePartDto part, string accessToken, CancellationToken cancellationToken = default);

    Task<PartAdminItemDto> UpdatePartAsync(int id, SavePartDto part, string accessToken, CancellationToken cancellationToken = default);

    Task<PartDeleteDto> DeletePartAsync(int id, string accessToken, CancellationToken cancellationToken = default);
}
