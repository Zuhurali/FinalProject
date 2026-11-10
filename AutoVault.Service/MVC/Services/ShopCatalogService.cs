using FinalProject.MVC.Commerce;
using FinalProject.MVC.Repositories;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Services;

public sealed class ShopCatalogService : IShopCatalogService
{
    private readonly IShopCatalogRepository _db;
    private readonly ICouponService _coupons;

    public ShopCatalogService(IShopCatalogRepository db)
        : this(db, new CouponService((ICouponRepository)db))
    {
    }

    public ShopCatalogService(IShopCatalogRepository db, ICouponService coupons)
    {
        _db = db;
        _coupons = coupons;
    }

    public async Task<IReadOnlyList<ShopProduct>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _db.GetActiveShopProductsOrderedAsync(cancellationToken);
        var liveOffers = await LoadLiveCampaignsAsync(DateTime.UtcNow, cancellationToken);
        return products.Select(product => ToModel(product, PickOffer(product, liveOffers))).ToList();
    }

    public async Task<ShopCatalogProduct?> GetRecordAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.GetShopProductByIdAsync(id, cancellationToken);
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
        _db.AddShopProduct(product);
        await _db.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task<bool> UpdateAsync(
        ShopCatalogProduct product,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.FindTrackedShopProductAsync(product.Id, cancellationToken);
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
            var affected = await _db.TryUpdateShopProductQuantityAsync(
                existing.Id,
                expectedQuantity,
                newQuantity,
                existing,
                cancellationToken);
            if (affected != 1)
            {
                throw new InvalidOperationException(
                    "Stock changed since this part was loaded (likely from a sale). Refresh and try again.");
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _db.FindTrackedShopProductAsync(id, cancellationToken);
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

        _db.RemoveShopProduct(existing);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<int> CountDependentsAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.CountShopProductDependentsAsync(id, cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _db.CountShopProductsAsync(cancellationToken);
    }

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.AnyShopProductsAsync(cancellationToken))
        {
            return;
        }

        await _db.SeedShopProductsWithIdentityInsertAsync(TemplateRecords().ToList(), cancellationToken);
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
        return await _db.GetAllCampaignsWithProductAsync(cancellationToken);
    }

    public Task<IReadOnlyList<ProductCampaign>> GetLiveCampaignsAsync(CancellationToken cancellationToken = default) =>
        LoadLiveCampaignsAsync(DateTime.UtcNow, cancellationToken);

    public async Task<ProductCampaign?> GetCampaignAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.GetCampaignByIdWithProductAsync(id, cancellationToken);
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

            product = await _db.GetActiveShopProductByIdAsync(campaign.ProductId.Value, cancellationToken);
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
            _db.AddProductCampaign(campaign);
        }
        else
        {
            var existing = await _db.FindTrackedProductCampaignAsync(campaign.Id, cancellationToken);
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
        var campaign = await _db.FindTrackedProductCampaignAsync(id, cancellationToken);
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
        var campaign = await _db.FindTrackedProductCampaignAsync(id, cancellationToken);
        if (campaign == null)
        {
            return (false, "Campaign not found.");
        }

        try
        {
            // InPersonSales.CampaignId is SetNull, so sale history is kept without cascading deletes.
            _db.RemoveProductCampaign(campaign);
            await _db.SaveChangesAsync(cancellationToken);
            return (true, null);
        }
        catch (Exception)
        {
            return (false, "The campaign could not be deleted because related records still reference it.");
        }
    }

    private async Task<ProductCampaign?> FindOverlappingActiveCampaignAsync(
        ProductCampaign campaign,
        CancellationToken cancellationToken)
    {
        var candidates = await _db.GetOtherActiveCampaignsWithProductAsync(campaign.Id, cancellationToken);
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
        return await _db.GetRecordedSalesWithDetailsAsync(cancellationToken);
    }

    public async Task<InPersonSale?> GetSaleAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.GetSaleByIdWithDetailsAsync(id, cancellationToken);
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
            var prior = await _db.FindSaleByIdempotencyKeyAsync(durableKey, cancellationToken);
            if (prior != null)
            {
                return (true, null);
            }
        }

        var product = await _db.GetShopProductByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            return (false, "Product not found.");
        }

        if (!product.IsActive)
        {
            return (false, "Inactive products cannot be sold in store.");
        }

        var error = ShowroomRequestRules.ValidateSaleAgainstReservations(quantity, product.Quantity, openReservedQuantity: 0);
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

        await using var transaction = await _db.BeginTransactionAsync(cancellationToken);
        var affected = await _db.DecrementShopProductStockAsync(productId, quantity, cancellationToken);
        if (affected != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "That sale is larger than the available quantity.");
        }

        _db.AddInPersonSale(new InPersonSale
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
                    _db.ClearChangeTracker();
                    return (false, redeemError ?? "Coupon could not be redeemed.");
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, null);
        }
        catch (Exception) when (durableKey != null)
        {
            await transaction.RollbackAsync(cancellationToken);
            _db.ClearChangeTracker();
            var raced = await _db.FindSaleByIdempotencyKeyAsync(durableKey, cancellationToken);
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
        await using var transaction = await _db.BeginTransactionAsync(cancellationToken);

        var claimed = await _db.ClaimVoidInPersonSaleAsync(saleId, voidedAt, voidedBy, cancellationToken);
        if (claimed != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "Sale not found or already voided.");
        }

        var sale = await _db.GetSaleByIdRequiredAsync(saleId, cancellationToken);

        var affected = await _db.IncrementShopProductStockAsync(sale.ProductId, sale.Quantity, cancellationToken);
        if (affected != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "Could not restore stock for that sale.");
        }

        await transaction.CommitAsync(cancellationToken);
        return (true, null);
    }

    public Task<IReadOnlyList<ProductInquiry>> GetInquiriesAsync(
        string? status = null,
        CancellationToken cancellationToken = default) =>
        _db.GetProductInquiriesAsync(status, cancellationToken);

    public async Task<IReadOnlyList<ProductInquiry>> GetAnsweredInquiriesForProductAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.GetAnsweredProductInquiriesAsync(productId, cancellationToken);

        // Public surface should not carry contact PII from the admin inbox.
        foreach (var inquiry in rows)
        {
            inquiry.Email = string.Empty;
            inquiry.Phone = string.Empty;
        }

        return rows;
    }

    public Task<ProductInquiry?> GetInquiryAsync(int id, CancellationToken cancellationToken = default) =>
        _db.GetProductInquiryByIdWithProductAsync(id, cancellationToken);

    public async Task<IReadOnlyList<ProductInquiry>> GetInquiriesForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<ProductInquiry>();
        }

        return await _db.GetProductInquiriesForUserAsync(userId, cancellationToken);
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
        var orphans = await _db.FindOrphanedProductInquiriesByEmailAsync(targetEmail, cancellationToken);
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

        if (!await _db.AnyActiveShopProductAsync(inquiry.ProductId, cancellationToken))
        {
            return (false, "Product not found.");
        }

        inquiry.UserId = string.IsNullOrWhiteSpace(inquiry.UserId) ? null : inquiry.UserId.Trim();
        var closed = await FindLatestClosedInquiryAsync(inquiry, cancellationToken);
        if (closed != null)
        {
            var followUpError = closed.TryAppendFollowUp(inquiry.Message);
            if (followUpError != null)
            {
                return (false, followUpError);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return (true, null);
        }

        inquiry.Id = 0;
        inquiry.Product = null;
        inquiry.CreatedAt = DateTime.UtcNow;
        inquiry.UpdatedAt = null;
        inquiry.IsHandled = false;
        inquiry.AdminReply = null;
        inquiry.AdminRepliedAt = null;
        inquiry.AdminUserId = null;
        _db.AddProductInquiry(inquiry);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    private async Task<ProductInquiry?> FindLatestClosedInquiryAsync(
        ProductInquiry inquiry,
        CancellationToken cancellationToken)
    {
        ProductInquiry? latest = null;
        if (!string.IsNullOrWhiteSpace(inquiry.UserId))
        {
            var mine = await _db.GetProductInquiriesForUserAsync(inquiry.UserId, cancellationToken);
            latest = mine.FirstOrDefault(item => item.ProductId == inquiry.ProductId);
        }
        else
        {
            var orphans = await _db.FindOrphanedProductInquiriesByEmailAsync(inquiry.Email.ToLowerInvariant(), cancellationToken);
            latest = orphans
                .Where(item => item.ProductId == inquiry.ProductId
                    && string.Equals(item.Email, inquiry.Email, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id)
                .FirstOrDefault();
        }

        if (latest is not { IsHandled: true })
        {
            return null;
        }

        return await _db.FindTrackedProductInquiryAsync(latest.Id, cancellationToken);
    }

    public async Task<bool> SetInquiryHandledAsync(int id, bool handled, CancellationToken cancellationToken = default)
    {
        var inquiry = await _db.FindTrackedProductInquiryAsync(id, cancellationToken);
        if (inquiry == null)
        {
            return false;
        }

        if (handled)
        {
            inquiry.MarkClosed();
        }
        else
        {
            inquiry.MarkReopened();
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
        var inquiry = await _db.FindTrackedProductInquiryAsync(id, cancellationToken);
        if (inquiry == null)
        {
            return (false, "Question not found.");
        }

        var previousReply = (inquiry.AdminReply ?? string.Empty).Trim();
        var replyError = inquiry.TrySaveReply(reply, adminUserId);
        if (replyError != null)
        {
            return (false, replyError);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var savedReply = (inquiry.AdminReply ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(inquiry.UserId)
            && !string.Equals(previousReply, savedReply, StringComparison.Ordinal))
        {
            var preview = savedReply.Length > 120 ? savedReply[..117] + "..." : savedReply;
            var dedupKey = $"inquiry_reply:{inquiry.Id}:{inquiry.AdminRepliedAt:O}";
            var body = $"AutoVault Support: {preview}";
            try
            {
                await _db.TryInsertInquiryReplyNotificationAsync(
                    inquiry.UserId,
                    inquiry.Id,
                    dedupKey,
                    body,
                    DateTime.UtcNow,
                    cancellationToken);
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
        var inquiry = await _db.FindTrackedProductInquiryAsync(id, cancellationToken);
        if (inquiry == null)
        {
            return false;
        }

        _db.RemoveProductInquiry(inquiry);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
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

    private Task<IReadOnlyList<ProductCampaign>> LoadLiveCampaignsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        _db.GetLiveCampaignsAsync(utcNow, cancellationToken);

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
