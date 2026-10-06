using FinalProject.DTOs;

namespace FinalProject.Services.Interfaces;

public interface ICatalogAdminService
{
    Task<AdminSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<BrandResponse?> CreateBrandAsync(string name, CancellationToken cancellationToken = default);

    Task<BrandResponse?> UpdateBrandAsync(int id, string name, CancellationToken cancellationToken = default);

    Task<bool> DeleteBrandAsync(int id, CancellationToken cancellationToken = default);

    Task<CategoryResponse?> CreateCategoryAsync(string name, CancellationToken cancellationToken = default);

    Task<CategoryResponse?> UpdateCategoryAsync(int id, string name, CancellationToken cancellationToken = default);

    Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
}
