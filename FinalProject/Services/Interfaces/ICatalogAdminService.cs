using FinalProject.DTOs;

namespace FinalProject.Services.Interfaces;

public interface ICatalogAdminService
{
    Task<AdminSummaryResponseDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<BrandResponseDto?> CreateBrandAsync(string name, CancellationToken cancellationToken = default);

    Task<BrandResponseDto?> UpdateBrandAsync(int id, string name, CancellationToken cancellationToken = default);

    Task<bool> DeleteBrandAsync(int id, CancellationToken cancellationToken = default);

    Task<CategoryResponseDto?> CreateCategoryAsync(string name, CancellationToken cancellationToken = default);

    Task<CategoryResponseDto?> UpdateCategoryAsync(int id, string name, CancellationToken cancellationToken = default);

    Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
}
