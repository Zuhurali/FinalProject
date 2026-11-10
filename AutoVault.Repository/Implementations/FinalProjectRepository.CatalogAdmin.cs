using FinalProject.Models.Entities;
using FinalProject.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Repositories.Implementations;

public sealed partial class FinalProjectRepository
{
    public async Task<CatalogAdminCountsRow> GetCountsAsync(CancellationToken cancellationToken = default)
    {
        var totalCars = await _db.Cars.CountAsync(cancellationToken);
        var activeListings = await _db.Cars.CountAsync(car => car.Status == "Active", cancellationToken);
        var totalConversations = await _db.Conversations.CountAsync(cancellationToken);
        var unreadMessages = await _db.Messages.CountAsync(message => !message.IsRead, cancellationToken);
        var totalRatings = await _db.ProductRatings.CountAsync(cancellationToken);
        var totalReviews = await _db.ProductReviews.CountAsync(cancellationToken);
        return new CatalogAdminCountsRow(
            totalCars,
            activeListings,
            totalConversations,
            unreadMessages,
            totalRatings,
            totalReviews);
    }

    public async Task<IReadOnlyList<CatalogRecentCarRow>> GetRecentCarsAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _db.Cars.AsNoTracking()
            .OrderByDescending(car => car.CreatedAt)
            .Take(take)
            .Select(car => new CatalogRecentCarRow(car.CreatedAt, car.Year, car.Brand.Name, car.Model))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CatalogRecentMessageRow>> GetRecentMessagesAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _db.Messages.AsNoTracking()
            .OrderByDescending(message => message.CreatedAt)
            .Take(take)
            .Select(message => new CatalogRecentMessageRow(message.CreatedAt, message.ConversationId))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> BrandNameExistsAsync(
        string name,
        int? excludeId = null,
        CancellationToken cancellationToken = default) =>
        excludeId is null
            ? _db.Brands.AnyAsync(brand => brand.Name == name, cancellationToken)
            : _db.Brands.AnyAsync(item => item.Id != excludeId && item.Name == name, cancellationToken);

    public Task<Brand?> GetBrandTrackedAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Brands.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<Brand> AddBrandAsync(Brand brand, CancellationToken cancellationToken = default)
    {
        _db.Brands.Add(brand);
        await _db.SaveChangesAsync(cancellationToken);
        return brand;
    }

    public async Task UpdateBrandAsync(Brand brand, CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> BrandHasCarsAsync(int brandId, CancellationToken cancellationToken = default) =>
        _db.Cars.AnyAsync(car => car.BrandId == brandId, cancellationToken);

    public async Task<bool> DeleteBrandAsync(Brand brand, CancellationToken cancellationToken = default)
    {
        _db.Brands.Remove(brand);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> CategoryNameExistsAsync(
        string name,
        int? excludeId = null,
        CancellationToken cancellationToken = default) =>
        excludeId is null
            ? _db.Categories.AnyAsync(category => category.Name == name, cancellationToken)
            : _db.Categories.AnyAsync(item => item.Id != excludeId && item.Name == name, cancellationToken);

    public Task<Category?> GetCategoryTrackedAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Categories.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<Category> AddCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task UpdateCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> CategoryHasCarsAsync(int categoryId, CancellationToken cancellationToken = default) =>
        _db.Cars.AnyAsync(car => car.CategoryId == categoryId, cancellationToken);

    public async Task<bool> DeleteCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
