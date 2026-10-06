using FinalProject.MVC.Controllers;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Services;

public sealed class ShopCatalogService : IShopCatalogService
{
    private readonly ApplicationDbContext _db;

    public ShopCatalogService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ShopProduct>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _db.ShopProducts.AsNoTracking()
            .OrderBy(product => product.Id)
            .ToListAsync(cancellationToken);
        return products.Select(ToModel).ToList();
    }

    public async Task<ShopCatalogProduct?> GetRecordAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.ShopProducts.AsNoTracking()
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
    }

    public async Task<ShopCatalogProduct> CreateAsync(
        ShopCatalogProduct product,
        CancellationToken cancellationToken = default)
    {
        product.Id = 0;
        product.SearchKeywords = Keywords(product);
        _db.ShopProducts.Add(product);
        await _db.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task<bool> UpdateAsync(
        ShopCatalogProduct product,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.ShopProducts.FirstOrDefaultAsync(item => item.Id == product.Id, cancellationToken);
        if (existing == null)
        {
            return false;
        }

        existing.Name = product.Name.Trim();
        existing.Price = product.Price;
        existing.Category = product.Category.Trim();
        existing.CarMake = product.CarMake.Trim();
        existing.CarModel = product.CarModel.Trim();
        existing.ImagePath = product.ImagePath.Trim();
        existing.Description = product.Description.Trim();
        existing.InStock = product.InStock;
        existing.Color = product.Color.Trim();
        existing.Fuel = product.Fuel.Trim();
        existing.SearchKeywords = Keywords(existing);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _db.ShopProducts.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (existing == null)
        {
            return false;
        }

        _db.ShopProducts.Remove(existing);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _db.ShopProducts.CountAsync(cancellationToken);
    }

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.ShopProducts.AnyAsync(cancellationToken))
        {
            return;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await _db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT ShopProducts ON", cancellationToken);
        _db.ShopProducts.AddRange(TemplateRecords());
        await _db.SaveChangesAsync(cancellationToken);
        await _db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT ShopProducts OFF", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public static ShopProduct ToModel(ShopCatalogProduct product)
    {
        return new ShopProduct
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Category = product.Category,
            CarMake = product.CarMake,
            CarModel = product.CarModel,
            ImagePath = product.ImagePath,
            Description = product.Description,
            InStock = product.InStock,
            Color = product.Color,
            Fuel = product.Fuel,
            SearchKeywords = product.SearchKeywords
        };
    }

    public static string Keywords(ShopCatalogProduct product)
    {
        return string.Join(" ", new[] { product.Name, product.Category, product.CarMake, product.CarModel }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    public static IReadOnlyList<ShopCatalogProduct> TemplateRecords()
    {
        return ShopController.TemplateProducts().Select(product => new ShopCatalogProduct
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Category = product.Category,
            CarMake = product.CarMake,
            CarModel = product.CarModel,
            ImagePath = product.ImagePath,
            Description = product.Description,
            InStock = product.InStock,
            Color = product.Color,
            Fuel = product.Fuel,
            SearchKeywords = product.SearchKeywords
        }).ToList();
    }
}
