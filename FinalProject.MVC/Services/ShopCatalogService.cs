using FinalProject.MVC.Commerce;
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
        var offers = await ActiveOffersAsync(DateTime.UtcNow, cancellationToken);
        return products.Select(product => ToModel(product, offers.GetValueOrDefault(product.Id))).ToList();
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
        if (product.Quantity < 0)
        {
            product.Quantity = 0;
        }

        product.InStock = product.Quantity > 0;
        product.Brand = product.Brand.Trim();
        product.Compatibility = product.Compatibility.Trim();
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
        existing.Quantity = Math.Max(0, product.Quantity);
        existing.InStock = existing.Quantity > 0;
        existing.Brand = product.Brand.Trim();
        existing.Compatibility = product.Compatibility.Trim();
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

    public static ShopProduct ToModel(ShopCatalogProduct product, ProductCampaign? offer = null)
    {
        var model = new ShopProduct
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Quantity = product.Quantity,
            StockStatus = ShowroomRules.StockStatus(Math.Max(0, product.Quantity)),
            Brand = product.Brand,
            Compatibility = product.Compatibility,
            Category = product.Category,
            CarMake = product.CarMake,
            CarModel = product.CarModel,
            ImagePath = product.ImagePath,
            Description = product.Description,
            InStock = product.Quantity > 0,
            Color = product.Color,
            Fuel = product.Fuel,
            SearchKeywords = product.SearchKeywords
        };

        if (offer != null &&
            ShowroomRules.OfferIsVisible(offer.IsActive, offer.StartsAt, offer.EndsAt, DateTime.UtcNow))
        {
            model.HasOffer = true;
            model.SalePrice = ShowroomRules.DiscountedPrice(product.Price, offer.DiscountedPrice, offer.DiscountPercent);
            model.DiscountPercent = ShowroomRules.BadgePercent(product.Price, model.SalePrice, offer.DiscountPercent);
            model.OfferEndsAt = offer.EndsAt;
            model.OfferName = offer.Name;
        }

        return model;
    }

    public static string Keywords(ShopCatalogProduct product)
    {
        return string.Join(" ", new[]
            {
                product.Name,
                product.Brand,
                product.Category,
                product.Compatibility,
                product.CarMake,
                product.CarModel
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static IReadOnlyList<ShopCatalogProduct> TemplateRecords()
    {
        return
        [
            Part(1, "Gear and Workshop Equipment Set", 400, 24, "11.webp", "Accessories", "Gear, shaft, and workshop equipment photographed for the showroom counter."),
            Part(2, "Brake and Suspension Parts Kit", 599, 18, "12.webp", "Brakes", "Brake, suspension, and filter parts shown together. Fitment is not stated."),
            Part(3, "Performance Tire", 399, 7, "13.webp", "Accessories", "A performance tire. Vehicle fitment is not stated."),
            Part(4, "Micrometer with Check Ring", 790, 3, "14.webp", "Workshop Tools", "Outside micrometer, bore gauges, and check ring."),
            Part(5, "Exhaust Mufflers and Pipes", 270, 0, "15.webp", "Accessories", "Exhaust mufflers, pipes, and a turbo assembly. Fitment is not stated."),
            Part(6, "Machine Gear and Bearing", 699, 42, "16.webp", "Accessories", "Machine gear and bearing."),
            Part(7, "Engine Assembly", 2490, 12, "parts/engine-assembly.webp", "Accessories", "Complete engine assembly. Vehicle application is not stated."),
            Part(8, "Brake Caliper and Rotor", 549, 4, "parts/brake-caliper.webp", "Brakes", "Red brake caliper on a drilled rotor. Pad and vehicle fitment are not stated."),
            Part(9, "Coilover Shock Absorber", 429, 31, "parts/coilover-shock.webp", "Accessories", "Coilover shock absorber with a red spring. Vehicle fitment is not stated."),
            Part(10, "Pleated Engine Air Filter", 49, 16, "parts/air-filter.webp", "Air Filters", "Pleated engine air filter. Vehicle fitment is not stated."),
            Part(11, "Spin-On Oil Filter", 28, 2, "parts/oil-filter.webp", "Oil Filters", "Spin-on oil filter. Vehicle fitment is not stated."),
            Part(12, "Timing Belt", 89, 9, "parts/timing-belt.webp", "Accessories", "Engine timing belt. Vehicle fitment is not stated."),
            Part(13, "Ball Bearing", 64, 1, "parts/ball-bearing.webp", "Accessories", "Ball bearing. Application is not stated."),
            Part(14, "Bore Gauge Set", 340, 55, "parts/bore-gauge.webp", "Workshop Tools", "Bore gauge set for engine measurement."),
            Part(15, "Precision Check Ring", 155, 0, "parts/check-ring.webp", "Workshop Tools", "Precision check ring for gauge setup."),
            Part(16, "Turbocharger Assembly", 1290, 22, "parts/turbocharger.webp", "Accessories", "Turbocharger and exhaust housing. Vehicle fitment is not stated."),
            Part(17, "Exhaust Muffler", 320, 6, "parts/exhaust-muffler.webp", "Accessories", "Exhaust muffler. Vehicle fitment is not stated."),
            Part(18, "Exhaust Tip", 180, 5, "parts/exhaust-tip.webp", "Accessories", "Exhaust tip. Vehicle fitment is not stated."),
            Part(19, "Splined Driveshaft", 260, 11, "parts/splined-shaft.webp", "Accessories", "Splined driveshaft. Vehicle fitment is not stated."),
            Part(20, "Gear Shaft and Fastener Kit", 175, 0, "parts/gear-shaft.webp", "Accessories", "Gear shafts, nuts, and fasteners."),
            Part(21, "Outside Micrometer", 220, 38, "parts/outside-micrometer.webp", "Workshop Tools", "Outside micrometer.")
        ];
    }

    private static ShopCatalogProduct Part(
        int id,
        string name,
        decimal price,
        int quantity,
        string image,
        string category,
        string description)
    {
        var product = new ShopCatalogProduct
        {
            Id = id,
            Name = name,
            Price = price,
            Quantity = quantity,
            Category = category,
            ImagePath = "~/assets/images/portfolio/" + image,
            Description = description,
            InStock = quantity > 0
        };
        product.SearchKeywords = Keywords(product);
        return product;
    }

    public async Task<IReadOnlyList<ProductCampaign>> GetCampaignsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.ProductCampaigns.AsNoTracking()
            .Include(campaign => campaign.Product)
            .OrderByDescending(campaign => campaign.StartsAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Ok, string? Error)> SaveCampaignAsync(
        ProductCampaign campaign,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.ShopProducts.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == campaign.ProductId, cancellationToken);
        if (product == null)
        {
            return (false, "Choose a product from the catalog.");
        }

        var error = ShowroomRules.ValidateCampaign(
            product.Price,
            campaign.DiscountedPrice,
            campaign.DiscountPercent,
            campaign.StartsAt,
            campaign.EndsAt);
        if (error != null)
        {
            return (false, error);
        }

        campaign.Name = campaign.Name.Trim();
        if (campaign.Id == 0)
        {
            _db.ProductCampaigns.Add(campaign);
        }
        else
        {
            var existing = await _db.ProductCampaigns
                .FirstOrDefaultAsync(item => item.Id == campaign.Id, cancellationToken);
            if (existing == null)
            {
                return (false, "Campaign not found.");
            }

            existing.ProductId = campaign.ProductId;
            existing.Name = campaign.Name;
            existing.DiscountedPrice = campaign.DiscountedPrice;
            existing.DiscountPercent = campaign.DiscountPercent;
            existing.StartsAt = campaign.StartsAt;
            existing.EndsAt = campaign.EndsAt;
            existing.IsActive = campaign.IsActive;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<bool> SetCampaignActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var campaign = await _db.ProductCampaigns.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (campaign == null)
        {
            return false;
        }

        campaign.IsActive = isActive;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<InPersonSale>> GetSalesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.InPersonSales.AsNoTracking()
            .Include(sale => sale.Product)
            .OrderByDescending(sale => sale.SoldAt)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Ok, string? Error)> RecordSaleAsync(
        int productId,
        int quantity,
        string recordedByUserId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.ShopProducts.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == productId, cancellationToken);
        if (product == null)
        {
            return (false, "Product not found.");
        }

        var error = ShowroomRules.ValidateSaleQuantity(quantity, product.Quantity);
        if (error != null)
        {
            return (false, error);
        }

        var offers = await ActiveOffersAsync(DateTime.UtcNow, cancellationToken);
        var priced = ToModel(product, offers.GetValueOrDefault(product.Id));
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var affected = await _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE ShopProducts
            SET Quantity = Quantity - {quantity},
                InStock = CASE WHEN Quantity - {quantity} > 0 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
            WHERE Id = {productId} AND Quantity >= {quantity}
            """, cancellationToken);
        if (affected != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "That sale is larger than the available quantity.");
        }

        _db.InPersonSales.Add(new InPersonSale
        {
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = priced.EffectivePrice,
            RecordedByUserId = recordedByUserId,
            Note = (note ?? string.Empty).Trim(),
            SoldAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (true, null);
    }

    public async Task<IReadOnlyList<ProductInquiry>> GetInquiriesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.ProductInquiries.AsNoTracking()
            .Include(inquiry => inquiry.Product)
            .OrderByDescending(inquiry => inquiry.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Ok, string? Error)> SaveInquiryAsync(
        ProductInquiry inquiry,
        CancellationToken cancellationToken = default)
    {
        var error = ProductInquiryRules.Validate(inquiry);
        if (error != null)
        {
            return (false, error);
        }

        var productExists = await _db.ShopProducts.AsNoTracking()
            .AnyAsync(product => product.Id == inquiry.ProductId, cancellationToken);
        if (!productExists)
        {
            return (false, "Product not found.");
        }

        inquiry.Id = 0;
        inquiry.CreatedAt = DateTime.UtcNow;
        inquiry.IsHandled = false;
        _db.ProductInquiries.Add(inquiry);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<bool> SetInquiryHandledAsync(int id, bool handled, CancellationToken cancellationToken = default)
    {
        var inquiry = await _db.ProductInquiries.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (inquiry == null)
        {
            return false;
        }

        inquiry.IsHandled = handled;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<Dictionary<int, ProductCampaign>> ActiveOffersAsync(
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var campaigns = await _db.ProductCampaigns.AsNoTracking()
            .Include(campaign => campaign.Product)
            .Where(campaign => campaign.IsActive && campaign.StartsAt <= utcNow && campaign.EndsAt > utcNow)
            .ToListAsync(cancellationToken);
        return campaigns
            .GroupBy(campaign => campaign.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(item => ShowroomRules.DiscountedPrice(
                    item.Product?.Price ?? 0,
                    item.DiscountedPrice,
                    item.DiscountPercent)).First());
    }
}
