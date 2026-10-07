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

    private static IReadOnlyList<ShopCatalogProduct> TemplateRecords()
    {
        return
        [
            Part(1, "Car Parts Gears and Equipment", 400, "11.webp", "Parts", "Porsche", "911", "Gear, shaft, and workshop equipment set."),
            Part(2, "Brake and Suspension Parts Kit", 599, "12.webp", "Brakes", "Mercedes-Benz", "AMG GT", "Brake caliper, drilled rotor, coilover, filters, timing belt, and bearings."),
            Part(3, "High-Performance Tires", 399, "13.webp", "Tires", "Toyota", "Highlander", "High-performance tire."),
            Part(4, "Micrometer with check ring", 790, "14.webp", "Tools", "", "", "Outside micrometer, bore gauges, and check ring."),
            Part(5, "Car Parts Mufflers And Pipes", 270, "15.webp", "Parts", "Nissan", "350Z", "Exhaust mufflers, pipes, and turbo assembly."),
            Part(6, "Car Gears Machine part", 699, "16.webp", "Parts", "Lamborghini", "Diablo", "Machine gear and bearing."),
            Part(7, "Engine Assembly", 2490, "parts/engine-assembly.webp", "Parts", "Ferrari", "California", "Complete engine assembly."),
            Part(8, "Brake Caliper and Rotor", 549, "parts/brake-caliper.webp", "Brakes", "Porsche", "911", "Red brake caliper on a drilled rotor."),
            Part(9, "Coilover Shock Absorber", 429, "parts/coilover-shock.webp", "Suspension", "Nissan", "350Z", "Coilover shock absorber with red spring."),
            Part(10, "Pleated Engine Air Filter", 49, "parts/air-filter.webp", "Filters", "Toyota", "Highlander", "Pleated engine air filter."),
            Part(11, "Spin-On Oil Filter", 28, "parts/oil-filter.webp", "Filters", "Tesla", "Model 3", "Spin-on oil filter."),
            Part(12, "Timing Belt", 89, "parts/timing-belt.webp", "Parts", "Maserati", "GranTurismo", "Engine timing belt."),
            Part(13, "Ball Bearing", 64, "parts/ball-bearing.webp", "Parts", "Mercedes-Benz", "SL-Class", "Ball bearing."),
            Part(14, "Bore Gauge Set", 340, "parts/bore-gauge.webp", "Tools", "", "", "Bore gauge set for engine measurement."),
            Part(15, "Precision Check Ring", 155, "parts/check-ring.webp", "Tools", "", "", "Precision check ring for gauge setup."),
            Part(16, "Turbocharger Assembly", 1290, "parts/turbocharger.webp", "Parts", "Nissan", "350Z", "Turbocharger and exhaust housing."),
            Part(17, "Exhaust Muffler", 320, "parts/exhaust-muffler.webp", "Parts", "Porsche", "911", "Exhaust muffler."),
            Part(18, "Exhaust Tip", 180, "parts/exhaust-tip.webp", "Parts", "Mercedes-Benz", "SL-Class", "Exhaust tip."),
            Part(19, "Splined Driveshaft", 260, "parts/splined-shaft.webp", "Parts", "Toyota", "Highlander", "Splined driveshaft."),
            Part(20, "Gear Shaft and Fastener Kit", 175, "parts/gear-shaft.webp", "Parts", "Rolls-Royce", "Phantom", "Gear shafts, nuts, and fasteners."),
            Part(21, "Outside Micrometer", 220, "parts/outside-micrometer.webp", "Tools", "", "", "Outside micrometer.")
        ];
    }

    private static ShopCatalogProduct Part(
        int id,
        string name,
        decimal price,
        string image,
        string category,
        string make,
        string model,
        string description)
    {
        var product = new ShopCatalogProduct
        {
            Id = id,
            Name = name,
            Price = price,
            Category = category,
            CarMake = make,
            CarModel = model,
            ImagePath = "~/assets/images/portfolio/" + image,
            Description = description,
            InStock = true
        };
        product.SearchKeywords = Keywords(product);
        return product;
    }
}
