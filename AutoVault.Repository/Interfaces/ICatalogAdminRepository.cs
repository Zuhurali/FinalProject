using FinalProject.Models.Entities;

namespace FinalProject.Repositories;

public sealed record CatalogRecentCarRow(DateTime CreatedAt, int Year, string Brand, string Model);
public sealed record CatalogRecentMessageRow(DateTime CreatedAt, int ConversationId);

public sealed record CatalogAdminCountsRow(
    int TotalCars,
    int ActiveListings,
    int TotalConversations,
    int UnreadMessages,
    int TotalRatings,
    int TotalReviews);

public interface ICatalogAdminRepository
{
    Task<CatalogAdminCountsRow> GetCountsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CatalogRecentCarRow>> GetRecentCarsAsync(int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CatalogRecentMessageRow>> GetRecentMessagesAsync(int take, CancellationToken cancellationToken = default);

    Task<bool> BrandNameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<Brand?> GetBrandTrackedAsync(int id, CancellationToken cancellationToken = default);
    Task<Brand> AddBrandAsync(Brand brand, CancellationToken cancellationToken = default);
    Task UpdateBrandAsync(Brand brand, CancellationToken cancellationToken = default);
    Task<bool> BrandHasCarsAsync(int brandId, CancellationToken cancellationToken = default);
    Task<bool> DeleteBrandAsync(Brand brand, CancellationToken cancellationToken = default);

    Task<bool> CategoryNameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<Category?> GetCategoryTrackedAsync(int id, CancellationToken cancellationToken = default);
    Task<Category> AddCategoryAsync(Category category, CancellationToken cancellationToken = default);
    Task UpdateCategoryAsync(Category category, CancellationToken cancellationToken = default);
    Task<bool> CategoryHasCarsAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryAsync(Category category, CancellationToken cancellationToken = default);
}
