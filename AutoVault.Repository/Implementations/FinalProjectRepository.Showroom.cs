using FinalProject.Models.Entities;
using FinalProject.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Repositories.Implementations;

public sealed partial class FinalProjectRepository
{
    Task IShowroomRepository.SaveChangesAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);

    private const string StatusNew = "New";
    private const string StatusContacted = "Contacted";
    private const string StatusCompleted = "Completed";

    public async Task<IReadOnlyList<ShowroomPurchaseRequestReference>> ListPurchaseRequestsAsync(
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ShowroomPurchaseRequests.AsNoTracking()
            .Include(request => request.Product)
            .AsQueryable();

        var allowedStatuses = new[] { StatusNew, StatusContacted, StatusCompleted, "Cancelled" };
        if (!string.IsNullOrWhiteSpace(status) && allowedStatuses.Contains(status))
        {
            query = query.Where(request => request.Status == status);
        }

        return await query
            .OrderBy(request => request.Status == StatusNew
                ? 0
                : request.Status == StatusContacted ? 1 : 2)
            .ThenByDescending(request => request.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
    }

    public Task<ShowroomPurchaseRequestReference?> GetPurchaseRequestWithProductAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        _db.ShowroomPurchaseRequests.AsNoTracking()
            .Include(request => request.Product)
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken);

    public Task<ShowroomPurchaseRequestReference?> GetPurchaseRequestTrackedAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        _db.ShowroomPurchaseRequests.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<ShowroomPurchaseRequestReference?> GetPurchaseRequestByIdAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        _db.ShowroomPurchaseRequests.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<bool> DeletePurchaseRequestAsync(int id, CancellationToken cancellationToken = default) =>
        DeletePurchaseRequestInternalAsync(id, cancellationToken);

    private async Task<bool> DeletePurchaseRequestInternalAsync(int id, CancellationToken cancellationToken)
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

    public Task<InPersonSaleReference?> FindActiveSaleByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        _db.InPersonSales.AsNoTracking()
            .Include(sale => sale.Product)
            .FirstOrDefaultAsync(sale => sale.IdempotencyKey == idempotencyKey && sale.VoidedAt == null, cancellationToken);

    public Task<ShopProductReference?> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default) =>
        _db.ShopProducts.AsNoTracking().FirstOrDefaultAsync(item => item.Id == productId, cancellationToken);

    public async Task<int> SumOpenReservedQuantityAsync(
        int productId,
        int? excludeRequestId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ShowroomPurchaseRequests.AsNoTracking()
            .Where(item => item.ProductId == productId
                && (item.Status == StatusNew || item.Status == StatusContacted));
        if (excludeRequestId is int excludeId)
        {
            query = query.Where(item => item.Id != excludeId);
        }

        return await query.SumAsync(item => (int?)item.Quantity, cancellationToken) ?? 0;
    }

    public async Task<ShowroomSalePriceRow> ResolveSalePriceAsync(
        ShopProductReference product,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var live = await _db.ProductCampaigns.AsNoTracking()
            .Where(campaign => campaign.IsActive && campaign.StartsAt <= utcNow && campaign.EndsAt > utcNow)
            .OrderByDescending(campaign => campaign.StartsAt)
            .ThenBy(campaign => campaign.Id)
            .ToListAsync(cancellationToken);

        var offer = live
            .Where(campaign =>
                !campaign.AppliesToAllParts
                && campaign.ProductId.HasValue
                && campaign.ProductId.Value == product.Id)
            .OrderBy(campaign => campaign.Id)
            .FirstOrDefault()
            ?? live
                .Where(campaign => campaign.AppliesToAllParts)
                .OrderBy(campaign => DiscountedPrice(
                    product.Price,
                    campaign.DiscountedPrice,
                    campaign.DiscountPercent))
                .ThenBy(campaign => campaign.Id)
                .FirstOrDefault();

        if (offer == null)
        {
            return new ShowroomSalePriceRow(product.Price, product.Price, null, null);
        }

        var unit = DiscountedPrice(product.Price, offer.DiscountedPrice, offer.DiscountPercent);
        var percent = BadgePercent(product.Price, unit, offer.DiscountPercent);
        return new ShowroomSalePriceRow(unit, product.Price, offer.Id, percent);
    }

    public async Task<ShowroomRecordSaleResult> RecordInPersonSaleAsync(
        int productId,
        int quantity,
        string recordedByUserId,
        string note,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (idempotencyKey != null)
        {
            var prior = await FindActiveSaleByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (prior != null)
            {
                return new ShowroomRecordSaleResult(true, null, prior, prior.Product);
            }
        }

        var product = await GetProductByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            return new ShowroomRecordSaleResult(false, "Product not found.", null, null);
        }

        if (!product.IsActive)
        {
            return new ShowroomRecordSaleResult(false, "Inactive products cannot be sold in store.", null, null);
        }

        var price = await ResolveSalePriceAsync(product, cancellationToken);
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
            return new ShowroomRecordSaleResult(false, "That sale is larger than the available quantity.", null, null);
        }

        var sale = new InPersonSaleReference
        {
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = price.UnitPrice,
            ListUnitPrice = price.ListPrice,
            CampaignId = price.CampaignId,
            CampaignDiscountPercent = price.CampaignPercent,
            RecordedByUserId = recordedByUserId.Trim(),
            Note = note,
            IdempotencyKey = idempotencyKey,
            SoldAt = DateTime.UtcNow,
            IsDemo = false
        };
        _db.InPersonSales.Add(sale);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException) when (idempotencyKey != null)
        {
            await transaction.RollbackAsync(cancellationToken);
            _db.ChangeTracker.Clear();
            var raced = await FindActiveSaleByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            return raced == null
                ? new ShowroomRecordSaleResult(false, "The sale could not be recorded.", null, null)
                : new ShowroomRecordSaleResult(true, null, raced, raced.Product);
        }

        sale.Product = product;
        return new ShowroomRecordSaleResult(true, null, sale, product);
    }

    public async Task<ShowroomCompleteWithSaleResult> CompletePurchaseRequestWithSaleAsync(
        int requestId,
        string recordedByUserId,
        string? note,
        string completeIdempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var priorSale = await FindActiveSaleByIdempotencyKeyAsync(completeIdempotencyKey, cancellationToken);
        if (priorSale != null)
        {
            return new ShowroomCompleteWithSaleResult(true, null, priorSale, priorSale.Product, null, false);
        }

        var completedAt = DateTime.UtcNow;
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var claimed = await _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE ShowroomPurchaseRequests
            SET Status = {StatusCompleted}, UpdatedAt = {completedAt}
            WHERE Id = {requestId}
              AND (Status = {StatusNew} OR Status = {StatusContacted})
            """, cancellationToken);
        if (claimed != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            var existing = await GetPurchaseRequestByIdAsync(requestId, cancellationToken);
            if (existing == null)
            {
                return new ShowroomCompleteWithSaleResult(false, "Request not found.", null, null, null, true);
            }

            priorSale = await FindActiveSaleByIdempotencyKeyAsync(completeIdempotencyKey, cancellationToken);
            if (priorSale != null)
            {
                return new ShowroomCompleteWithSaleResult(true, null, priorSale, priorSale.Product, existing.Status, false);
            }

            return new ShowroomCompleteWithSaleResult(
                false,
                $"Request #{requestId} is {existing.Status} and cannot be completed with a sale.",
                null,
                null,
                existing.Status,
                false);
        }

        var request = await _db.ShowroomPurchaseRequests.AsNoTracking()
            .FirstAsync(item => item.Id == requestId, cancellationToken);

        var product = await GetProductByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new ShowroomCompleteWithSaleResult(false, "Product not found.", null, null, null, false);
        }

        if (!product.IsActive)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new ShowroomCompleteWithSaleResult(
                false,
                "Inactive products cannot be sold in store.",
                null,
                null,
                null,
                false);
        }

        if (request.Quantity < 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new ShowroomCompleteWithSaleResult(false, "Enter a quantity of at least 1.", null, null, null, false);
        }

        if (request.Quantity > product.Quantity)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new ShowroomCompleteWithSaleResult(
                false,
                "That sale is larger than the available quantity.",
                null,
                null,
                null,
                false);
        }

        var price = await ResolveSalePriceAsync(product, cancellationToken);
        var affected = await _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE ShopProducts
            SET Quantity = Quantity - {request.Quantity},
                InStock = CASE WHEN Quantity - {request.Quantity} > 0 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
            WHERE Id = {request.ProductId} AND Quantity >= {request.Quantity}
            """, cancellationToken);
        if (affected != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new ShowroomCompleteWithSaleResult(
                false,
                "That sale is larger than the available quantity.",
                null,
                null,
                null,
                false);
        }

        var saleNote = string.IsNullOrWhiteSpace(note)
            ? $"From purchase request #{request.Id}"
            : note.Trim();
        var sale = new InPersonSaleReference
        {
            ProductId = request.ProductId,
            PurchaseRequestId = request.Id,
            Quantity = request.Quantity,
            UnitPrice = price.UnitPrice,
            ListUnitPrice = price.ListPrice,
            CampaignId = price.CampaignId,
            CampaignDiscountPercent = price.CampaignPercent,
            RecordedByUserId = recordedByUserId.Trim(),
            Note = saleNote,
            IdempotencyKey = completeIdempotencyKey,
            SoldAt = completedAt,
            IsDemo = false
        };
        _db.InPersonSales.Add(sale);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            _db.ChangeTracker.Clear();
            priorSale = await FindActiveSaleByIdempotencyKeyAsync(completeIdempotencyKey, cancellationToken);
            return priorSale == null
                ? new ShowroomCompleteWithSaleResult(false, "The sale could not be recorded.", null, null, null, false)
                : new ShowroomCompleteWithSaleResult(true, null, priorSale, priorSale.Product, null, false);
        }

        sale.Product = product;
        return new ShowroomCompleteWithSaleResult(true, null, sale, product, null, false);
    }

    public async Task<IReadOnlyList<InPersonSaleReference>> ListRecentSalesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.InPersonSales.AsNoTracking()
            .Where(sale => !sale.IsDemo && sale.VoidedAt == null)
            .Include(sale => sale.Product)
            .OrderByDescending(sale => sale.SoldAt)
            .Take(200)
            .ToListAsync(cancellationToken);
    }

    public async Task<ShowroomVoidSaleResult> VoidSaleAsync(
        int saleId,
        string voidedByUserId,
        CancellationToken cancellationToken = default)
    {
        var voidedBy = voidedByUserId.Trim();
        var voidedAt = DateTime.UtcNow;
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var claimed = await _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE InPersonSales
            SET VoidedAt = {voidedAt}, VoidedByUserId = {voidedBy}
            WHERE Id = {saleId} AND IsDemo = 0 AND VoidedAt IS NULL
            """, cancellationToken);
        if (claimed != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new ShowroomVoidSaleResult(false, "Sale not found or already voided.");
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
            return new ShowroomVoidSaleResult(false, "Could not restore stock for that sale.");
        }

        await transaction.CommitAsync(cancellationToken);
        return new ShowroomVoidSaleResult(true, null);
    }

    private static decimal DiscountedPrice(decimal originalPrice, decimal? discountedPrice, decimal? discountPercent)
    {
        if (discountedPrice.HasValue)
        {
            return decimal.Round(discountedPrice.Value, 2, MidpointRounding.AwayFromZero);
        }

        var percent = discountPercent ?? 0;
        return decimal.Round(originalPrice * (1m - percent / 100m), 2, MidpointRounding.AwayFromZero);
    }

    private static int? BadgePercent(decimal originalPrice, decimal salePrice, decimal? discountPercent)
    {
        if (discountPercent.HasValue)
        {
            return (int)decimal.Round(discountPercent.Value, 0, MidpointRounding.AwayFromZero);
        }

        if (originalPrice <= 0 || salePrice >= originalPrice)
        {
            return null;
        }

        return (int)decimal.Round((1m - salePrice / originalPrice) * 100m, 0, MidpointRounding.AwayFromZero);
    }
}
