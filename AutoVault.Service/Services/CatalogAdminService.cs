using FinalProject.Repositories;
using FinalProject.DTOs;
using FinalProject.Models.Entities;
using FinalProject.Services.Interfaces;

namespace FinalProject.Services;

public sealed class CatalogAdminService : ICatalogAdminService
{
    private readonly ICatalogAdminRepository _db;

    public CatalogAdminService(ICatalogAdminRepository db)
    {
        _db = db;
    }

    public async Task<AdminSummaryResponseDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var counts = await _db.GetCountsAsync(cancellationToken);
        var recentCars = await _db.GetRecentCarsAsync(5, cancellationToken);
        var recentMessages = await _db.GetRecentMessagesAsync(5, cancellationToken);

        var activity = recentCars
            .Select(car => (car.CreatedAt, Text: $"{car.CreatedAt:yyyy-MM-dd} · Listing · {car.Year} {car.Brand} {car.Model}"))
            .Concat(recentMessages.Select(message =>
                (message.CreatedAt, Text: $"{message.CreatedAt:yyyy-MM-dd} · Message · conversation #{message.ConversationId}")))
            .OrderByDescending(item => item.CreatedAt)
            .Take(8)
            .Select(item => item.Text)
            .ToList();

        return new AdminSummaryResponseDto(
            counts.TotalCars,
            counts.ActiveListings,
            counts.TotalConversations,
            counts.UnreadMessages,
            counts.TotalRatings,
            counts.TotalReviews,
            activity);
    }

    public async Task<BrandResponseDto?> CreateBrandAsync(string name, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)
            || await _db.BrandNameExistsAsync(trimmed, cancellationToken: cancellationToken))
        {
            return null;
        }

        var brand = new Brand { Name = trimmed, CreatedAt = DateTime.UtcNow };
        await _db.AddBrandAsync(brand, cancellationToken);
        return new BrandResponseDto { Id = brand.Id, Name = brand.Name };
    }

    public async Task<BrandResponseDto?> UpdateBrandAsync(int id, string name, CancellationToken cancellationToken = default)
    {
        var brand = await _db.GetBrandTrackedAsync(id, cancellationToken);
        var trimmed = name.Trim();
        if (brand == null
            || string.IsNullOrWhiteSpace(trimmed)
            || await _db.BrandNameExistsAsync(trimmed, excludeId: id, cancellationToken: cancellationToken))
        {
            return null;
        }

        brand.Name = trimmed;
        await _db.UpdateBrandAsync(brand, cancellationToken);
        return new BrandResponseDto { Id = brand.Id, Name = brand.Name };
    }

    public async Task<bool> DeleteBrandAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _db.BrandHasCarsAsync(id, cancellationToken))
        {
            return false;
        }

        var brand = await _db.GetBrandTrackedAsync(id, cancellationToken);
        if (brand == null)
        {
            return false;
        }

        return await _db.DeleteBrandAsync(brand, cancellationToken);
    }

    public async Task<CategoryResponseDto?> CreateCategoryAsync(string name, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)
            || await _db.CategoryNameExistsAsync(trimmed, cancellationToken: cancellationToken))
        {
            return null;
        }

        var category = new Category { Name = trimmed, IconPath = string.Empty };
        await _db.AddCategoryAsync(category, cancellationToken);
        return ToCategoryResponse(category);
    }

    public async Task<CategoryResponseDto?> UpdateCategoryAsync(int id, string name, CancellationToken cancellationToken = default)
    {
        var category = await _db.GetCategoryTrackedAsync(id, cancellationToken);
        var trimmed = name.Trim();
        if (category == null
            || string.IsNullOrWhiteSpace(trimmed)
            || await _db.CategoryNameExistsAsync(trimmed, excludeId: id, cancellationToken: cancellationToken))
        {
            return null;
        }

        category.Name = trimmed;
        await _db.UpdateCategoryAsync(category, cancellationToken);
        return ToCategoryResponse(category);
    }

    public async Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _db.CategoryHasCarsAsync(id, cancellationToken))
        {
            return false;
        }

        var category = await _db.GetCategoryTrackedAsync(id, cancellationToken);
        if (category == null)
        {
            return false;
        }

        return await _db.DeleteCategoryAsync(category, cancellationToken);
    }

    private static CategoryResponseDto ToCategoryResponse(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        IconPath = category.IconPath ?? string.Empty
    };
}
