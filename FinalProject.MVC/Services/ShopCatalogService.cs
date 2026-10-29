using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Services;

public sealed class ShopCatalogService : IShopCatalogService
{
    private readonly ApplicationDbContext _db;
    private readonly ICouponService _coupons;

    public ShopCatalogService(ApplicationDbContext db)
        : this(db, new CouponService(db))
    {
    }

    public ShopCatalogService(ApplicationDbContext db, ICouponService coupons)
    {
        _db = db;
        _coupons = coupons;
    }

    public async Task<IReadOnlyList<ShopProduct>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _db.ShopProducts.AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.Id)
            .ToListAsync(cancellationToken);
        var liveOffers = await LoadLiveCampaignsAsync(DateTime.UtcNow, cancellationToken);
        return products.Select(product => ToModel(product, PickOffer(product, liveOffers))).ToList();
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
        if (product.Quantity < 0 || product.Price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(product), "Price and stock cannot be negative.");
        }

        product.InStock = product.Quantity > 0;
        product.IsActive = true;
        if (product.CreatedAt == default)
        {
            product.CreatedAt = DateTime.UtcNow;
        }
        product.Brand = product.Brand.Trim();
        product.PartNumber = product.PartNumber.Trim();
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

        if (product.Quantity < 0 || product.Price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(product), "Price and stock cannot be negative.");
        }

        var expectedQuantity = existing.Quantity;
        var newQuantity = product.Quantity;

        existing.Name = product.Name.Trim();
        existing.Price = product.Price;
        existing.Category = product.Category.Trim();
        existing.CarMake = product.CarMake.Trim();
        existing.CarModel = product.CarModel.Trim();
        existing.ImagePath = product.ImagePath.Trim();
        existing.Description = product.Description.Trim();
        existing.IsActive = product.IsActive;
        existing.Brand = product.Brand.Trim();
        existing.PartNumber = product.PartNumber.Trim();
        existing.Compatibility = product.Compatibility.Trim();
        existing.Color = product.Color.Trim();
        existing.Fuel = product.Fuel.Trim();
        existing.SearchKeywords = Keywords(existing);

        if (newQuantity != expectedQuantity)
        {
            // Optimistic claim — refuse absolute stock edits that race a concurrent sale.
            var affected = await _db.ShopProducts
                .Where(item => item.Id == existing.Id && item.Quantity == expectedQuantity)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(item => item.Quantity, newQuantity)
                        .SetProperty(item => item.InStock, newQuantity > 0),
                    cancellationToken);
            if (affected != 1)
            {
                throw new InvalidOperationException(
                    "Stock changed since this part was loaded (likely from a sale). Refresh and try again.");
            }

            // Avoid EF reverting CurrentValue when clearing IsModified.
            _db.Entry(existing).Property(item => item.Quantity).OriginalValue = newQuantity;
            _db.Entry(existing).Property(item => item.Quantity).IsModified = false;
            _db.Entry(existing).Property(item => item.InStock).OriginalValue = newQuantity > 0;
            _db.Entry(existing).Property(item => item.InStock).IsModified = false;
            existing.Quantity = newQuantity;
            existing.InStock = newQuantity > 0;
        }

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

        var dependents = await CountDependentsAsync(id, cancellationToken);
        if (dependents > 0)
        {
            existing.IsActive = false;
            existing.InStock = existing.Quantity > 0;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        _db.ShopProducts.Remove(existing);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<int> CountDependentsAsync(int id, CancellationToken cancellationToken)
    {
        var campaigns = await _db.ProductCampaigns.CountAsync(item => item.ProductId == id, cancellationToken);
        var sales = await _db.InPersonSales.CountAsync(item => item.ProductId == id, cancellationToken);
        var inquiries = await _db.ProductInquiries.CountAsync(item => item.ProductId == id, cancellationToken);
        var requests = await _db.ShowroomPurchaseRequests.CountAsync(item => item.ProductId == id, cancellationToken);
        var feedback = await _db.Database.SqlQueryRaw<int>(
            """
            SELECT
                (SELECT COUNT(*) FROM ProductRatings WHERE ProductId = {0})
                + (SELECT COUNT(*) FROM ProductReviews WHERE ProductId = {0})
                + (SELECT COUNT(*) FROM ProductFavorites WHERE ProductId = {0}) AS Value
            """,
            id).ToListAsync(cancellationToken);
        return campaigns + sales + inquiries + requests + feedback.FirstOrDefault();
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
            PartNumber = product.PartNumber,
            Compatibility = product.Compatibility,
            Category = product.Category,
            CarMake = product.CarMake,
            CarModel = product.CarModel,
            ImagePath = product.ImagePath,
            Description = product.Description,
            InStock = product.Quantity > 0,
            Color = product.Color,
            Fuel = product.Fuel,
            SearchKeywords = product.SearchKeywords,
            CreatedAt = product.CreatedAt
        };

        if (offer != null &&
            ShowroomRules.OfferIsVisible(offer.IsActive, offer.StartsAt, offer.EndsAt, DateTime.UtcNow))
        {
            model.HasOffer = true;
            model.SalePrice = ShowroomRules.DiscountedPrice(product.Price, offer.DiscountedPrice, offer.DiscountPercent);
            model.DiscountPercent = ShowroomRules.BadgePercent(product.Price, model.SalePrice, offer.DiscountPercent);
            model.OfferEndsAt = offer.EndsAt;
            model.OfferEndLabel = ShowroomRules.InclusiveEndLabel(offer.EndsAt);
            model.OfferName = offer.Name;
            model.OfferCampaignId = offer.Id;
        }

        return model;
    }

    public static string Keywords(ShopCatalogProduct product)
    {
        return string.Join(" ", new[]
            {
                product.Name,
                product.Brand,
                product.PartNumber,
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
            Part(1, "Gear and Workshop Equipment Set", 400, 24, "11.webp", "Accessories", "Gear, shaft, and workshop equipment photographed for the store counter."),
            Part(2, "Brake and Suspension Parts Kit", 599, 18, "12.webp", "Brakes", "Brake, suspension, and filter parts shown together. Fitment is not stated."),
            Part(3, "Performance Tire", 399, 7, "13.webp", "Accessories", "A performance tire. Vehicle fitment is not stated."),
            Part(4, "Micrometer with Check Ring", 790, 3, "14.webp", "Workshop Tools", "Outside micrometer, bore gauges, and check ring."),
            Part(5, "Exhaust Mufflers and Pipes", 270, 0, "15.webp", "Accessories", "Exhaust mufflers, pipes, and a turbo assembly. Fitment is not stated."),
            Part(6, "Machine Gear and Bearing", 699, 42, "16.webp", "Accessories", "Machine gear and bearing."),
            Part(7, "Engine Assembly", 2490, 12, "parts/hd/engine-assembly.webp", "Accessories", "Complete engine assembly. Vehicle application is not stated."),
            Part(8, "Brake Caliper and Rotor", 549, 4, "parts/hd/brake-caliper.webp", "Brakes", "Red brake caliper on a drilled rotor. Pad and vehicle fitment are not stated."),
            Part(9, "Coilover Shock Absorber", 429, 31, "parts/hd/coilover-shock.webp", "Accessories", "Coilover shock absorber. Vehicle fitment is not stated."),
            Part(10, "Pleated Engine Air Filter", 49, 16, "parts/hd/air-filter.webp", "Air Filters", "Pleated engine air filter. Vehicle fitment is not stated."),
            Part(11, "Spin-On Oil Filter", 28, 2, "parts/hd/oil-filter.webp", "Oil Filters", "Spin-on oil filter. Vehicle fitment is not stated."),
            Part(12, "Timing Belt", 89, 9, "parts/hd/timing-belt.webp", "Accessories", "Engine timing belt. Vehicle fitment is not stated."),
            Part(13, "Ball Bearing", 64, 1, "parts/hd/ball-bearing.webp", "Accessories", "Ball bearing. Application is not stated."),
            Part(14, "Bore Gauge Set", 340, 55, "parts/hd/bore-gauge.webp", "Workshop Tools", "Inside micrometer set for measuring engine bores."),
            Part(15, "Precision Check Ring", 155, 0, "parts/hd/check-ring.webp", "Workshop Tools", "Precision ring gauge for checking threads and setting gauges."),
            Part(16, "Turbocharger Assembly", 1290, 7, "parts/hd/turbocharger.webp", "Accessories", "Turbocharger and exhaust housing. Vehicle fitment is not stated."),
            Part(17, "Exhaust Muffler", 320, 6, "parts/hd/exhaust-muffler.webp", "Accessories", "Exhaust muffler. Vehicle fitment is not stated."),
            Part(18, "Exhaust Tip", 180, 5, "parts/hd/exhaust-tip.webp", "Accessories", "Exhaust tip. Vehicle fitment is not stated."),
            Part(19, "Splined Driveshaft", 260, 11, "parts/hd/splined-shaft.webp", "Accessories", "Splined driveshaft. Vehicle fitment is not stated."),
            Part(20, "Gear Shaft and Fastener Kit", 175, 0, "parts/hd/gear-shaft-fastener-kit.webp", "Accessories", "Gear shafts, nuts, and fasteners."),
            Part(21, "Outside Micrometer", 220, 38, "parts/hd/outside-micrometer.webp", "Workshop Tools", "Outside micrometer.")
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

    public Task<IReadOnlyList<ProductCampaign>> GetLiveCampaignsAsync(CancellationToken cancellationToken = default) =>
        LoadLiveCampaignsAsync(DateTime.UtcNow, cancellationToken);

    public async Task<ProductCampaign?> GetCampaignAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.ProductCampaigns.AsNoTracking()
            .Include(campaign => campaign.Product)
            .FirstOrDefaultAsync(campaign => campaign.Id == id, cancellationToken);
    }

    public async Task<(bool Ok, string? Error)> SaveCampaignAsync(
        ProductCampaign campaign,
        CancellationToken cancellationToken = default)
    {
        NormalizeCampaignScope(campaign);
        NormalizeCampaignOffer(campaign);
        NormalizeCampaignDates(campaign);

        ShopCatalogProduct? product = null;
        if (campaign.AppliesToAllParts)
        {
            var windowError = ShowroomRules.ValidateCampaignWindowAndPercent(
                campaign.DiscountPercent,
                campaign.DiscountedPrice,
                campaign.StartsAt,
                campaign.EndsAt);
            if (windowError != null)
            {
                return (false, windowError);
            }

            if (campaign.DiscountedPrice.HasValue)
            {
                return (false, "All-parts campaigns must use a percentage discount, not a fixed sale price.");
            }
        }
        else
        {
            if (campaign.ProductId is null or <= 0)
            {
                return (false, "Choose a spare part from the catalog.");
            }

            product = await _db.ShopProducts.AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.Id == campaign.ProductId.Value && item.IsActive,
                    cancellationToken);
            if (product == null)
            {
                return (false, "Choose an active spare part from the catalog.");
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
        }

        if (string.IsNullOrWhiteSpace(campaign.Name))
        {
            campaign.Name = campaign.AppliesToAllParts
                ? (campaign.DiscountPercent.HasValue
                    ? $"All spare parts · {campaign.DiscountPercent:0.#}% OFF"
                    : "All spare parts campaign")
                : (campaign.DiscountPercent.HasValue
                    ? $"{product!.Name} · {campaign.DiscountPercent:0.#}% OFF"
                    : $"{product!.Name} campaign");
        }
        else
        {
            campaign.Name = campaign.Name.Trim();
        }

        if (campaign.IsActive)
        {
            var overlap = await FindOverlappingActiveCampaignAsync(campaign, cancellationToken);
            if (overlap != null)
            {
                var target = overlap.AppliesToAllParts
                    ? "all spare parts"
                    : (overlap.Product?.Name ?? $"product #{overlap.ProductId}");
                return (false,
                    $"Another active campaign already covers {target} from {overlap.StartsAt:yyyy-MM-dd} to {overlap.EndsAt.AddDays(-1):yyyy-MM-dd} (UTC). Deactivate it or choose different dates.");
            }
        }

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
            existing.AppliesToAllParts = campaign.AppliesToAllParts;
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

    public async Task<(bool Ok, string? Error)> SetCampaignActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var campaign = await _db.ProductCampaigns.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (campaign == null)
        {
            return (false, "Campaign not found.");
        }

        if (isActive)
        {
            var overlap = await FindOverlappingActiveCampaignAsync(campaign, cancellationToken);
            if (overlap != null)
            {
                var target = overlap.AppliesToAllParts
                    ? "all spare parts"
                    : (overlap.Product?.Name ?? $"product #{overlap.ProductId}");
                return (false,
                    $"Another active campaign already covers {target} from {overlap.StartsAt:yyyy-MM-dd} to {overlap.EndsAt.AddDays(-1):yyyy-MM-dd} (UTC). Deactivate it first.");
            }
        }

        campaign.IsActive = isActive;
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DeleteCampaignAsync(int id, CancellationToken cancellationToken = default)
    {
        var campaign = await _db.ProductCampaigns.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (campaign == null)
        {
            return (false, "Campaign not found.");
        }

        try
        {
            // InPersonSales.CampaignId is SetNull, so sale history is kept without cascading deletes.
            _db.ProductCampaigns.Remove(campaign);
            await _db.SaveChangesAsync(cancellationToken);
            return (true, null);
        }
        catch (DbUpdateException)
        {
            return (false, "The campaign could not be deleted because related records still reference it.");
        }
    }

    private async Task<ProductCampaign?> FindOverlappingActiveCampaignAsync(
        ProductCampaign campaign,
        CancellationToken cancellationToken)
    {
        var candidates = await _db.ProductCampaigns.AsNoTracking()
            .Include(item => item.Product)
            .Where(item => item.IsActive && item.Id != campaign.Id)
            .ToListAsync(cancellationToken);
        return candidates.FirstOrDefault(item =>
            ShowroomRules.CampaignWindowsOverlap(item.StartsAt, item.EndsAt, campaign.StartsAt, campaign.EndsAt)
            && ShowroomRules.CampaignsConflict(
                campaign.AppliesToAllParts,
                campaign.ProductId,
                item.AppliesToAllParts,
                item.ProductId));
    }

    private static void NormalizeCampaignScope(ProductCampaign campaign)
    {
        if (campaign.AppliesToAllParts)
        {
            campaign.ProductId = null;
            campaign.DiscountedPrice = null;
        }
        else if (campaign.ProductId is <= 0)
        {
            campaign.ProductId = null;
        }
    }

    private static void NormalizeCampaignOffer(ProductCampaign campaign)
    {
        if (campaign.DiscountPercent.HasValue)
        {
            campaign.DiscountedPrice = null;
        }
        else if (campaign.DiscountedPrice.HasValue)
        {
            campaign.DiscountPercent = null;
        }
    }

    private static void NormalizeCampaignDates(ProductCampaign campaign)
    {
        // Admin form posts calendar dates labeled as UTC. End date is inclusive for the teacher;
        // storage uses an exclusive EndsAt at the next UTC midnight.
        var start = campaign.StartsAt.Date;
        var endInclusive = campaign.EndsAt.Date;
        campaign.StartsAt = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        campaign.EndsAt = DateTime.SpecifyKind(endInclusive.AddDays(1), DateTimeKind.Utc);
    }

    public async Task<IReadOnlyList<InPersonSale>> GetSalesAsync(CancellationToken cancellationToken = default)
    {
        // Recorded sales only — simulated/demo and voided rows are excluded from the admin history.
        return await _db.InPersonSales.AsNoTracking()
            .Where(sale => !sale.IsDemo && sale.VoidedAt == null)
            .Include(sale => sale.Product)
            .Include(sale => sale.Campaign)
            .OrderByDescending(sale => sale.SoldAt)
            .Take(200)
            .ToListAsync(cancellationToken);
    }

    public async Task<InPersonSale?> GetSaleAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.InPersonSales.AsNoTracking()
            .Include(sale => sale.Product)
            .Include(sale => sale.Campaign)
            .FirstOrDefaultAsync(sale => sale.Id == id, cancellationToken);
    }

    public async Task<(bool Ok, string? Error)> RecordSaleAsync(
        int productId,
        int quantity,
        string recordedByUserId,
        string? note,
        Guid? idempotencyKey = null,
        string? couponCode = null,
        string? customerUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recordedByUserId))
        {
            return (false, "Recorded-by user is required.");
        }

        var durableKey = idempotencyKey is { } key && key != Guid.Empty
            ? key.ToString("N")
            : null;
        if (durableKey != null)
        {
            var prior = await _db.InPersonSales.AsNoTracking()
                .FirstOrDefaultAsync(sale => sale.IdempotencyKey == durableKey, cancellationToken);
            if (prior != null)
            {
                return (true, null);
            }
        }

        var product = await _db.ShopProducts.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == productId, cancellationToken);
        if (product == null)
        {
            return (false, "Product not found.");
        }

        if (!product.IsActive)
        {
            return (false, "Inactive products cannot be sold in store.");
        }

        var openReserved = await SumOpenReservedAsync(productId, excludeRequestId: null, cancellationToken);
        var error = ShowroomRequestRules.ValidateSaleAgainstReservations(quantity, product.Quantity, openReserved);
        if (error != null)
        {
            return (false, error);
        }

        var liveOffers = await LoadLiveCampaignsAsync(DateTime.UtcNow, cancellationToken);
        var offer = PickOffer(product, liveOffers);
        var priced = ToModel(product, offer);

        var (couponOk, couponError, couponDiscount, normalizedCoupon) = await PreviewSaleCouponAsync(
            couponCode,
            customerUserId,
            product.Id,
            product.Name,
            priced.EffectivePrice,
            quantity,
            cancellationToken);
        if (!couponOk)
        {
            return (false, couponError);
        }

        var saleNote = (note ?? string.Empty).Trim();
        if (normalizedCoupon != null)
        {
            var couponNote = $"Coupon {normalizedCoupon} redeemed in store.";
            saleNote = string.IsNullOrEmpty(saleNote) ? couponNote : $"{saleNote} {couponNote}";
            if (saleNote.Length > 500)
            {
                saleNote = saleNote[..500];
            }
        }

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
            ListUnitPrice = product.Price,
            CampaignId = offer?.Id,
            CampaignDiscountPercent = priced.HasOffer && priced.DiscountPercent.HasValue
                ? priced.DiscountPercent.Value
                : null,
            RecordedByUserId = recordedByUserId.Trim(),
            Note = saleNote,
            IdempotencyKey = durableKey,
            SoldAt = DateTime.UtcNow,
            IsDemo = false
        });
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            if (normalizedCoupon != null)
            {
                var (redeemed, redeemError) = await _coupons.RedeemAsync(
                    customerUserId!,
                    normalizedCoupon,
                    couponDiscount,
                    cancellationToken);
                if (!redeemed)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _db.ChangeTracker.Clear();
                    return (false, redeemError ?? "Coupon could not be redeemed.");
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, null);
        }
        catch (DbUpdateException) when (durableKey != null)
        {
            await transaction.RollbackAsync(cancellationToken);
            _db.ChangeTracker.Clear();
            var raced = await _db.InPersonSales.AsNoTracking()
                .FirstOrDefaultAsync(sale => sale.IdempotencyKey == durableKey, cancellationToken);
            return raced != null
                ? (true, null)
                : (false, "The sale could not be recorded.");
        }
    }

    public async Task<(bool Ok, string? Error)> VoidSaleAsync(
        int saleId,
        string voidedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(voidedByUserId))
        {
            return (false, "Voided-by user is required.");
        }

        var voidedBy = voidedByUserId.Trim();
        var voidedAt = DateTime.UtcNow;
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        // Exclusive claim — only one concurrent void wins.
        var claimed = await _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE InPersonSales
            SET VoidedAt = {voidedAt}, VoidedByUserId = {voidedBy}
            WHERE Id = {saleId} AND IsDemo = 0 AND VoidedAt IS NULL
            """, cancellationToken);
        if (claimed != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "Sale not found or already voided.");
        }

        var sale = await _db.InPersonSales.AsNoTracking()
            .FirstAsync(item => item.Id == saleId, cancellationToken);

        var affected = await _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE ShopProducts
            SET Quantity = Quantity + {sale.Quantity},
                InStock = CASE WHEN Quantity + {sale.Quantity} > 0 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
            WHERE Id = {sale.ProductId}
            """, cancellationToken);
        if (affected != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "Could not restore stock for that sale.");
        }

        if (sale.PurchaseRequestId is int requestId)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE ShowroomPurchaseRequests
                SET Status = {ShowroomRequestStatus.Contacted}, UpdatedAt = {voidedAt}
                WHERE Id = {requestId} AND Status = {ShowroomRequestStatus.Completed}
                """, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return (true, null);
    }

    public async Task<IReadOnlyList<ProductInquiry>> GetInquiriesAsync(
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ProductInquiries.AsNoTracking()
            .Include(inquiry => inquiry.Product)
            .AsQueryable();

        query = status?.Trim().ToLowerInvariant() switch
        {
            "new" => query.Where(inquiry =>
                !inquiry.IsHandled && (inquiry.AdminReply == null || inquiry.AdminReply == "")),
            "answered" => query.Where(inquiry =>
                inquiry.AdminReply != null && inquiry.AdminReply != ""),
            "closed" => query.Where(inquiry =>
                inquiry.IsHandled && (inquiry.AdminReply == null || inquiry.AdminReply == "")),
            _ => query
        };

        return await query
            .OrderByDescending(inquiry => inquiry.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductInquiry>> GetAnsweredInquiriesForProductAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.ProductInquiries.AsNoTracking()
            .Where(inquiry =>
                inquiry.ProductId == productId
                && inquiry.AdminReply != null
                && inquiry.AdminReply != "")
            .OrderByDescending(inquiry => inquiry.CreatedAt)
            .ThenByDescending(inquiry => inquiry.Id)
            .ToListAsync(cancellationToken);

        // Public surface should not carry contact PII from the admin inbox.
        foreach (var inquiry in rows)
        {
            inquiry.Email = string.Empty;
            inquiry.Phone = string.Empty;
        }

        return rows;
    }

    public async Task<ProductInquiry?> GetInquiryAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.ProductInquiries.AsNoTracking()
            .Include(inquiry => inquiry.Product)
            .FirstOrDefaultAsync(inquiry => inquiry.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductInquiry>> GetInquiriesForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<ProductInquiry>();
        }

        return await _db.ProductInquiries.AsNoTracking()
            .Include(inquiry => inquiry.Product)
            .Where(inquiry => inquiry.UserId == userId)
            .OrderByDescending(inquiry => inquiry.CreatedAt)
            .ThenByDescending(inquiry => inquiry.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ClaimOrphanedInquiriesByEmailAsync(
        string userId,
        string accountEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(accountEmail))
        {
            return 0;
        }

        var targetEmail = accountEmail.Trim().ToLowerInvariant();
        var orphans = await _db.ProductInquiries
            .Where(inquiry =>
                inquiry.UserId == null
                && inquiry.Email != null
                && inquiry.Email.ToLower() == targetEmail)
            .ToListAsync(cancellationToken);

        if (orphans.Count == 0)
        {
            return 0;
        }

        foreach (var orphan in orphans)
        {
            orphan.UserId = userId.Trim();
        }

        await _db.SaveChangesAsync(cancellationToken);
        return orphans.Count;
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
            .AnyAsync(product => product.Id == inquiry.ProductId && product.IsActive, cancellationToken);
        if (!productExists)
        {
            return (false, "Product not found.");
        }

        inquiry.Id = 0;
        inquiry.Product = null;
        inquiry.UserId = string.IsNullOrWhiteSpace(inquiry.UserId) ? null : inquiry.UserId.Trim();
        inquiry.CreatedAt = DateTime.UtcNow;
        inquiry.IsHandled = false;
        inquiry.AdminReply = null;
        inquiry.AdminRepliedAt = null;
        inquiry.AdminUserId = null;
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
        // When an admin reopens a question (handled=false) the existing reply is no longer
        // authoritative, so clear it so the public page does not show stale information.
        if (!handled)
        {
            inquiry.AdminReply = null;
            inquiry.AdminRepliedAt = null;
            inquiry.AdminUserId = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(bool Ok, string? Error)> SaveInquiryReplyAsync(
        int id,
        string reply,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = (reply ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            return (false, "Reply cannot be empty.");
        }

        if (normalized.Length > 2000)
        {
            return (false, "Reply cannot exceed 2 000 characters.");
        }

        var inquiry = await _db.ProductInquiries.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (inquiry == null)
        {
            return (false, "Question not found.");
        }

        inquiry.AdminReply = normalized;
        inquiry.AdminRepliedAt = DateTime.UtcNow;
        inquiry.AdminUserId = adminUserId;
        // A reply also counts as handled
        inquiry.IsHandled = true;
        await _db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(inquiry.UserId))
        {
            var preview = normalized.Length > 120 ? normalized[..117] + "..." : normalized;
            var dedupKey = $"inquiry_reply:{inquiry.Id}:{inquiry.AdminRepliedAt:O}";
            var body = $"Store reply: {preview}";
            var createdAt = DateTime.UtcNow;
            try
            {
                await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    IF NOT EXISTS (
                        SELECT 1 FROM UserNotifications
                        WHERE UserId = {inquiry.UserId} AND DedupKey = {dedupKey})
                    INSERT INTO UserNotifications
                        (UserId, [Type], Title, Body, Url, DedupKey, IsRead, CreatedAt)
                    VALUES
                        ({inquiry.UserId}, N'inquiry_reply', N'Reply to your question',
                         {body}, N'/Account/ProductQuestions', {dedupKey}, CAST(0 AS bit), {createdAt})
                    """, cancellationToken);
            }
            catch (Exception)
            {
                // Reply is already saved; notification is best-effort.
            }
        }

        return (true, null);
    }

    public async Task<bool> DeleteInquiryAsync(int id, CancellationToken cancellationToken = default)
    {
        var inquiry = await _db.ProductInquiries.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (inquiry == null)
        {
            return false;
        }

        _db.ProductInquiries.Remove(inquiry);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ShowroomPurchaseRequest>> GetPurchaseRequestsAsync(
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ShowroomPurchaseRequests.AsNoTracking()
            .Include(request => request.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && ShowroomRequestStatus.All.Contains(status))
        {
            query = query.Where(request => request.Status == status);
        }

        return await query
            .OrderBy(request => request.Status == ShowroomRequestStatus.New ? 0 : request.Status == ShowroomRequestStatus.Contacted ? 1 : 2)
            .ThenByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ShowroomPurchaseRequest?> GetPurchaseRequestAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.ShowroomPurchaseRequests.AsNoTracking()
            .Include(request => request.Product)
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ShowroomPurchaseRequest>> GetPurchaseRequestsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<ShowroomPurchaseRequest>();
        }

        return await _db.ShowroomPurchaseRequests.AsNoTracking()
            .Include(request => request.Product)
            .Where(request => request.UserId == userId)
            .OrderByDescending(request => request.CreatedAt)
            .ThenByDescending(request => request.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ClaimOrphanedPurchaseRequestsByEmailAsync(
        string userId,
        string accountEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(accountEmail))
        {
            return 0;
        }

        var targetEmail = accountEmail.Trim();
        var candidates = await _db.ShowroomPurchaseRequests
            .Where(request => request.UserId == null)
            .ToListAsync(cancellationToken);

        var orphans = candidates
            .Where(request =>
                !string.IsNullOrWhiteSpace(request.Email)
                && string.Equals(request.Email.Trim(), targetEmail, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (orphans.Count == 0)
        {
            return 0;
        }

        foreach (var orphan in orphans)
        {
            orphan.UserId = userId;
            orphan.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return orphans.Count;
    }

    public async Task<PurchaseRequestResult> SavePurchaseRequestAsync(
        ShowroomPurchaseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.SubmissionKey != Guid.Empty)
        {
            var existing = await _db.ShowroomPurchaseRequests.AsNoTracking()
                .FirstOrDefaultAsync(item => item.SubmissionKey == request.SubmissionKey, cancellationToken);
            if (existing != null)
            {
                return PurchaseRequestResult.AlreadyReceived(existing.Id);
            }
        }

        var product = await _db.ShopProducts.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.ProductId, cancellationToken);
        if (product == null || !product.IsActive)
        {
            return PurchaseRequestResult.Failed("Product not found.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var openRequest = await _db.ShowroomPurchaseRequests.AsNoTracking()
            .Where(item => item.ProductId == request.ProductId &&
                           item.Email.ToLower() == email &&
                           (item.Status == ShowroomRequestStatus.New || item.Status == ShowroomRequestStatus.Contacted))
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (openRequest != null)
        {
            return PurchaseRequestResult.Failed(
                $"You already have an open request (#{openRequest.Id}) for this product. Our team will contact you about it.");
        }

        var openReserved = await SumOpenReservedAsync(request.ProductId, excludeRequestId: null, cancellationToken);
        var available = ShowroomRequestRules.UnreservedQuantity(product.Quantity, openReserved);
        var error = ShowroomRequestRules.Validate(request, available, DateTime.Now);
        if (error != null)
        {
            return PurchaseRequestResult.Failed(error);
        }

        request.Id = 0;
        request.Product = null;
        request.CustomerName = request.CustomerName.Trim();
        // Normalize email so the open-request unique index matches case-insensitively.
        request.Email = email;
        request.Phone = request.Phone.Trim();
        request.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        request.UserId = string.IsNullOrWhiteSpace(request.UserId) ? null : request.UserId.Trim();
        request.Status = ShowroomRequestStatus.New;
        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = null;
        _db.ShowroomPurchaseRequests.Add(request);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            var saved = await _db.ShowroomPurchaseRequests.AsNoTracking()
                .FirstOrDefaultAsync(item => item.SubmissionKey == request.SubmissionKey, cancellationToken);
            if (saved != null)
            {
                return PurchaseRequestResult.AlreadyReceived(saved.Id);
            }

            var raced = await _db.ShowroomPurchaseRequests.AsNoTracking()
                .Where(item => item.ProductId == request.ProductId &&
                               item.Email == email &&
                               (item.Status == ShowroomRequestStatus.New || item.Status == ShowroomRequestStatus.Contacted))
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (raced != null)
            {
                return PurchaseRequestResult.Failed(
                    $"You already have an open request (#{raced.Id}) for this product. Our team will contact you about it.");
            }

            throw;
        }

        return PurchaseRequestResult.Created(request.Id);
    }

    public async Task<(bool Ok, string? Error)> SetPurchaseRequestStatusAsync(
        int id,
        string status,
        CancellationToken cancellationToken = default)
    {
        if (!ShowroomRequestStatus.All.Contains(status))
        {
            return (false, "Status is not allowed.");
        }

        var request = await _db.ShowroomPurchaseRequests.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (request == null)
        {
            return (false, "Request not found.");
        }

        if (!ShowroomRequestRules.CanTransition(request.Status, status))
        {
            return (false, $"Cannot change status from {request.Status} to {status}.");
        }

        request.Status = status;
        request.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> CompletePurchaseRequestWithSaleAsync(
        int requestId,
        string recordedByUserId,
        string? note,
        string? couponCode = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recordedByUserId))
        {
            return (false, "Recorded-by user is required.");
        }

        var completeKey = $"req-complete:{requestId}";
        var priorSale = await _db.InPersonSales.AsNoTracking()
            .FirstOrDefaultAsync(
                sale => sale.IdempotencyKey == completeKey && sale.VoidedAt == null,
                cancellationToken);
        if (priorSale != null)
        {
            return (true, null);
        }

        var completedAt = DateTime.UtcNow;
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        // Exclusive claim — only one concurrent complete wins.
        var openNew = ShowroomRequestStatus.New;
        var openContacted = ShowroomRequestStatus.Contacted;
        var claimed = await _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE ShowroomPurchaseRequests
            SET Status = {ShowroomRequestStatus.Completed}, UpdatedAt = {completedAt}
            WHERE Id = {requestId}
              AND (Status = {openNew} OR Status = {openContacted})
            """, cancellationToken);
        if (claimed != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            var existing = await _db.ShowroomPurchaseRequests.AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == requestId, cancellationToken);
            if (existing == null)
            {
                return (false, "Request not found.");
            }

            priorSale = await _db.InPersonSales.AsNoTracking()
                .FirstOrDefaultAsync(
                    sale => sale.IdempotencyKey == completeKey && sale.VoidedAt == null,
                    cancellationToken);
            if (priorSale != null)
            {
                return (true, null);
            }

            return (false, $"Request #{requestId} is {existing.Status} and cannot be completed with a sale.");
        }

        var request = await _db.ShowroomPurchaseRequests.AsNoTracking()
            .FirstAsync(item => item.Id == requestId, cancellationToken);

        async Task<(bool Ok, string? Error)> FailAfterClaimAsync(string error)
        {
            // Rollback undoes the exclusive Completed claim as well as stock changes.
            await transaction.RollbackAsync(cancellationToken);
            return (false, error);
        }

        var saleNote = string.IsNullOrWhiteSpace(note)
            ? $"From purchase request #{request.Id}"
            : note.Trim();

        var product = await _db.ShopProducts.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.ProductId, cancellationToken);
        if (product == null)
        {
            return await FailAfterClaimAsync("Product not found.");
        }

        if (!product.IsActive)
        {
            return await FailAfterClaimAsync("Inactive products cannot be sold in store.");
        }

        var qtyError = ShowroomRules.ValidateSaleQuantity(request.Quantity, product.Quantity);
        if (qtyError != null)
        {
            return await FailAfterClaimAsync(qtyError);
        }

        var liveOffers = await LoadLiveCampaignsAsync(DateTime.UtcNow, cancellationToken);
        var offer = PickOffer(product, liveOffers);
        var priced = ToModel(product, offer);

        var (couponOk, couponError, couponDiscount, normalizedCoupon) = await PreviewSaleCouponAsync(
            couponCode,
            request.UserId,
            product.Id,
            product.Name,
            priced.EffectivePrice,
            request.Quantity,
            cancellationToken);
        if (!couponOk)
        {
            return await FailAfterClaimAsync(couponError ?? "Coupon could not be applied.");
        }

        if (normalizedCoupon != null)
        {
            var couponNote = $"Coupon {normalizedCoupon} redeemed in store.";
            saleNote = string.IsNullOrEmpty(saleNote) ? couponNote : $"{saleNote} {couponNote}";
            if (saleNote.Length > 500)
            {
                saleNote = saleNote[..500];
            }
        }

        var affected = await _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE ShopProducts
            SET Quantity = Quantity - {request.Quantity},
                InStock = CASE WHEN Quantity - {request.Quantity} > 0 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
            WHERE Id = {request.ProductId} AND Quantity >= {request.Quantity}
            """, cancellationToken);
        if (affected != 1)
        {
            return await FailAfterClaimAsync("That sale is larger than the available quantity.");
        }

        _db.InPersonSales.Add(new InPersonSale
        {
            ProductId = request.ProductId,
            PurchaseRequestId = request.Id,
            Quantity = request.Quantity,
            UnitPrice = priced.EffectivePrice,
            ListUnitPrice = product.Price,
            CampaignId = offer?.Id,
            CampaignDiscountPercent = priced.HasOffer && priced.DiscountPercent.HasValue
                ? priced.DiscountPercent.Value
                : null,
            RecordedByUserId = recordedByUserId.Trim(),
            Note = saleNote,
            IdempotencyKey = completeKey,
            SoldAt = completedAt,
            IsDemo = false
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            if (normalizedCoupon != null)
            {
                var (redeemed, redeemError) = await _coupons.RedeemAsync(
                    request.UserId!,
                    normalizedCoupon,
                    couponDiscount,
                    cancellationToken);
                if (!redeemed)
                {
                    return await FailAfterClaimAsync(redeemError ?? "Coupon could not be redeemed.");
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, null);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            _db.ChangeTracker.Clear();
            priorSale = await _db.InPersonSales.AsNoTracking()
                .FirstOrDefaultAsync(
                    sale => sale.IdempotencyKey == completeKey && sale.VoidedAt == null,
                    cancellationToken);
            return priorSale != null
                ? (true, null)
                : (false, "The sale could not be recorded.");
        }
    }

    private async Task<(bool Ok, string? Error, decimal Discount, string? NormalizedCode)> PreviewSaleCouponAsync(
        string? couponCode,
        string? customerUserId,
        int productId,
        string productName,
        decimal unitPrice,
        int quantity,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            return (true, null, 0m, null);
        }

        if (string.IsNullOrWhiteSpace(customerUserId))
        {
            return (false, "A signed-in customer account is required to redeem a coupon in store.", 0m, null);
        }

        var cart = new CartViewModel
        {
            Items =
            {
                new CartItem
                {
                    ProductId = productId,
                    ProductName = productName,
                    Price = unitPrice,
                    Quantity = quantity
                }
            }
        };
        var (ok, message, discount, code) = await _coupons.PreviewApplyAsync(
            couponCode,
            customerUserId,
            cart,
            cancellationToken);
        if (!ok || code == null)
        {
            return (false, message, 0m, null);
        }

        return (true, null, discount, code);
    }

    private async Task<int> SumOpenReservedAsync(
        int productId,
        int? excludeRequestId,
        CancellationToken cancellationToken)
    {
        var query = _db.ShowroomPurchaseRequests.AsNoTracking()
            .Where(item => item.ProductId == productId
                && (item.Status == ShowroomRequestStatus.New || item.Status == ShowroomRequestStatus.Contacted));
        if (excludeRequestId is int excludeId)
        {
            query = query.Where(item => item.Id != excludeId);
        }

        return await query.SumAsync(item => (int?)item.Quantity, cancellationToken) ?? 0;
    }

    public async Task<bool> DeletePurchaseRequestAsync(int id, CancellationToken cancellationToken = default)
    {
        var request = await _db.ShowroomPurchaseRequests.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (request == null)
        {
            return false;
        }

        _db.ShowroomPurchaseRequests.Remove(request);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<IReadOnlyList<ProductCampaign>> LoadLiveCampaignsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return await _db.ProductCampaigns.AsNoTracking()
            .Include(campaign => campaign.Product)
            .Where(campaign => campaign.IsActive && campaign.StartsAt <= utcNow && campaign.EndsAt > utcNow)
            .OrderByDescending(campaign => campaign.StartsAt)
            .ThenBy(campaign => campaign.Id)
            .ToListAsync(cancellationToken);
    }

    private static ProductCampaign? PickOffer(
        ShopCatalogProduct product,
        IReadOnlyList<ProductCampaign> liveCampaigns)
    {
        // Never stack offers. A single-product campaign always wins for that product;
        // otherwise use at most one live all-parts campaign.
        var specific = liveCampaigns
            .Where(campaign =>
                !campaign.AppliesToAllParts
                && campaign.ProductId.HasValue
                && campaign.ProductId.Value == product.Id)
            .OrderBy(campaign => campaign.Id)
            .FirstOrDefault();
        if (specific != null)
        {
            return specific;
        }

        return liveCampaigns
            .Where(campaign => campaign.AppliesToAllParts)
            .OrderBy(campaign => ShowroomRules.DiscountedPrice(
                product.Price,
                campaign.DiscountedPrice,
                campaign.DiscountPercent))
            .ThenBy(campaign => campaign.Id)
            .FirstOrDefault();
    }
}
