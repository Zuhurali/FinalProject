using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.Models.Entities;
using FinalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Services;

public sealed class CatalogAdminService : ICatalogAdminService
{
    private readonly FinalProjectDbContext _db;

    public CatalogAdminService(FinalProjectDbContext db)
    {
        _db = db;
    }

    public async Task<AdminSummaryResponseDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalCars = await _db.Cars.CountAsync(cancellationToken);
        var activeListings = await _db.Cars.CountAsync(car => car.Status == "Active", cancellationToken);
        var totalConversations = await _db.Conversations.CountAsync(cancellationToken);
        var unreadMessages = await _db.Messages.CountAsync(message => !message.IsRead, cancellationToken);
        var totalRatings = await _db.ProductRatings.CountAsync(cancellationToken);
        var totalReviews = await _db.ProductReviews.CountAsync(cancellationToken);

        var recentCars = await _db.Cars.AsNoTracking()
            .OrderByDescending(car => car.CreatedAt)
            .Take(5)
            .Select(car => new { car.CreatedAt, car.Year, Brand = car.Brand.Name, car.Model })
            .ToListAsync(cancellationToken);
        var recentMessages = await _db.Messages.AsNoTracking()
            .OrderByDescending(message => message.CreatedAt)
            .Take(5)
            .Select(message => new { message.CreatedAt, message.ConversationId })
            .ToListAsync(cancellationToken);

        var activity = recentCars
            .Select(car => (car.CreatedAt, Text: $"{car.CreatedAt:yyyy-MM-dd} · Listing · {car.Year} {car.Brand} {car.Model}"))
            .Concat(recentMessages.Select(message =>
                (message.CreatedAt, Text: $"{message.CreatedAt:yyyy-MM-dd} · Message · conversation #{message.ConversationId}")))
            .OrderByDescending(item => item.CreatedAt)
            .Take(8)
            .Select(item => item.Text)
            .ToList();

        return new AdminSummaryResponseDto(
            totalCars,
            activeListings,
            totalConversations,
            unreadMessages,
            totalRatings,
            totalReviews,
            activity);
    }

    public async Task<BrandResponseDto?> CreateBrandAsync(string name, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)
            || await _db.Brands.AnyAsync(brand => brand.Name == trimmed, cancellationToken))
        {
            return null;
        }

        var brand = new Brand { Name = trimmed, CreatedAt = DateTime.UtcNow };
        _db.Brands.Add(brand);
        await _db.SaveChangesAsync(cancellationToken);
        return new BrandResponseDto { Id = brand.Id, Name = brand.Name };
    }

    public async Task<BrandResponseDto?> UpdateBrandAsync(int id, string name, CancellationToken cancellationToken = default)
    {
        var brand = await _db.Brands.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        var trimmed = name.Trim();
        if (brand == null
            || string.IsNullOrWhiteSpace(trimmed)
            || await _db.Brands.AnyAsync(item => item.Id != id && item.Name == trimmed, cancellationToken))
        {
            return null;
        }

        brand.Name = trimmed;
        await _db.SaveChangesAsync(cancellationToken);
        return new BrandResponseDto { Id = brand.Id, Name = brand.Name };
    }

    public async Task<bool> DeleteBrandAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _db.Cars.AnyAsync(car => car.BrandId == id, cancellationToken))
        {
            return false;
        }

        var brand = await _db.Brands.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (brand == null)
        {
            return false;
        }

        _db.Brands.Remove(brand);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CategoryResponseDto?> CreateCategoryAsync(string name, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)
            || await _db.Categories.AnyAsync(category => category.Name == trimmed, cancellationToken))
        {
            return null;
        }

        var category = new Category { Name = trimmed, IconPath = string.Empty };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        return ToCategoryResponse(category);
    }

    public async Task<CategoryResponseDto?> UpdateCategoryAsync(int id, string name, CancellationToken cancellationToken = default)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        var trimmed = name.Trim();
        if (category == null
            || string.IsNullOrWhiteSpace(trimmed)
            || await _db.Categories.AnyAsync(item => item.Id != id && item.Name == trimmed, cancellationToken))
        {
            return null;
        }

        category.Name = trimmed;
        await _db.SaveChangesAsync(cancellationToken);
        return ToCategoryResponse(category);
    }

    public async Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _db.Cars.AnyAsync(car => car.CategoryId == id, cancellationToken))
        {
            return false;
        }

        var category = await _db.Categories.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (category == null)
        {
            return false;
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static CategoryResponseDto ToCategoryResponse(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        IconPath = category.IconPath ?? string.Empty
    };
}
