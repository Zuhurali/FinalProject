using FinalProject.Commerce;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.Models.Entities;
using FinalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Services;

public sealed class ShowroomAdminService : IShowroomAdminService
{
    private readonly FinalProjectDbContext _db;

    public ShowroomAdminService(FinalProjectDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ShowroomRequestItem>> ListRequestsAsync(
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ShowroomPurchaseRequests.AsNoTracking()
            .Include(request => request.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && ShowroomApiRules.AllStatuses.Contains(status))
        {
            query = query.Where(request => request.Status == status);
        }

        var rows = await query
            .OrderBy(request => request.Status == ShowroomApiRules.StatusNew
                ? 0
                : request.Status == ShowroomApiRules.StatusContacted ? 1 : 2)
            .ThenByDescending(request => request.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return rows.Select(MapRequest).ToList();
    }

    public async Task<ShowroomRequestItem?> GetRequestAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await _db.ShowroomPurchaseRequests.AsNoTracking()
            .Include(request => request.Product)
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken);
        return row == null ? null : MapRequest(row);
    }

    public async Task<(ShowroomRequestItem? Item, string? Error)> SetRequestStatusAsync(
        int id,
        string status,
        CancellationToken cancellationToken = default)
    {
        if (!ShowroomApiRules.AllStatuses.Contains(status))
        {
            return (null, "Status is not allowed.");
        }

        var request = await _db.ShowroomPurchaseRequests.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (request == null)
        {
            return (null, "Request not found.");
        }

        if (!ShowroomApiRules.CanTransition(request.Status, status))
        {
            return (null, $"Cannot change status from {request.Status} to {status}.");
        }

        request.Status = status;
        request.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetRequestAsync(id, cancellationToken), null);
    }

    public async Task<(bool Found, string? Error)> DeleteRequestAsync(int id, CancellationToken cancellationToken = default)
    {
        var request = await _db.ShowroomPurchaseRequests.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (request == null)
        {
            return (false, "Request not found.");
        }

        _db.ShowroomPurchaseRequests.Remove(request);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(ShowroomSaleItem? Sale, string? Error)> RecordSaleAsync(
        RecordShowroomSaleRequest request,
        string recordedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recordedByUserId))
        {
            return (null, "Recorded-by user is required.");
        }

        var durableKey = request.IdempotencyKey is { } key && key != Guid.Empty
            ? key.ToString("N")
            : null;
        if (durableKey != null)
        {
            var prior = await _db.InPersonSales.AsNoTracking()
                .Include(sale => sale.Product)
                .FirstOrDefaultAsync(sale => sale.IdempotencyKey == durableKey, cancellationToken);
            if (prior != null)
            {
                return (MapSale(prior), null);
            }
        }

        var product = await _db.ShopProducts.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.ProductId, cancellationToken);
        if (product == null)
        {
            return (null, "Product not found.");
        }

        if (!product.IsActive)
        {
            return (null, "Inactive products cannot be sold in store.");
        }

        var openReserved = await SumOpenReservedAsync(request.ProductId, excludeRequestId: null, cancellationToken);
        var qtyError = ShowroomApiRules.ValidateSaleAgainstReservations(
            request.Quantity,
            product.Quantity,
            openReserved);
        if (qtyError != null)
        {
            return (null, qtyError);
        }

        var (unitPrice, listPrice, campaignId, campaignPercent) = await PriceAsync(product, cancellationToken);
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var affected = await _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE ShopProducts
            SET Quantity = Quantity - {request.Quantity},
                InStock = CASE WHEN Quantity - {request.Quantity} > 0 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
            WHERE Id = {request.ProductId} AND Quantity >= {request.Quantity}
            """, cancellationToken);
        if (affected != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (null, "That sale is larger than the available quantity.");
        }

        var sale = new InPersonSaleReference
        {
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            UnitPrice = unitPrice,
            ListUnitPrice = listPrice,
            CampaignId = campaignId,
            CampaignDiscountPercent = campaignPercent,
            RecordedByUserId = recordedByUserId.Trim(),
            Note = (request.Note ?? string.Empty).Trim(),
            IdempotencyKey = durableKey,
            SoldAt = DateTime.UtcNow,
            IsDemo = false
        };
        _db.InPersonSales.Add(sale);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException) when (durableKey != null)
        {
            await transaction.RollbackAsync(cancellationToken);
            _db.ChangeTracker.Clear();
            var raced = await _db.InPersonSales.AsNoTracking()
                .Include(item => item.Product)
                .FirstOrDefaultAsync(item => item.IdempotencyKey == durableKey, cancellationToken);
            return raced == null
                ? (null, "The sale could not be recorded.")
                : (MapSale(raced), null);
        }

        sale.Product = product;
        return (MapSale(sale), null);
    }

    public async Task<(ShowroomSaleItem? Sale, string? Error)> CompleteRequestWithSaleAsync(
        int requestId,
        string recordedByUserId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recordedByUserId))
        {
            return (null, "Recorded-by user is required.");
        }

        var completeKey = ShowroomApiRules.CompleteSaleIdempotencyKey(requestId);
        var priorSale = await _db.InPersonSales.AsNoTracking()
            .Include(sale => sale.Product)
            .FirstOrDefaultAsync(sale => sale.IdempotencyKey == completeKey && sale.VoidedAt == null, cancellationToken);
        if (priorSale != null)
        {
            return (MapSale(priorSale), null);
        }

        var completedAt = DateTime.UtcNow;
        var openNew = ShowroomApiRules.StatusNew;
        var openContacted = ShowroomApiRules.StatusContacted;
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var claimed = await _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE ShowroomPurchaseRequests
            SET Status = {ShowroomApiRules.StatusCompleted}, UpdatedAt = {completedAt}
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
                return (null, "Request not found.");
            }

            priorSale = await _db.InPersonSales.AsNoTracking()
                .Include(sale => sale.Product)
                .FirstOrDefaultAsync(sale => sale.IdempotencyKey == completeKey && sale.VoidedAt == null, cancellationToken);
            if (priorSale != null)
            {
                return (MapSale(priorSale), null);
            }

            return (null, $"Request #{requestId} is {existing.Status} and cannot be completed with a sale.");
        }

        var request = await _db.ShowroomPurchaseRequests.AsNoTracking()
            .FirstAsync(item => item.Id == requestId, cancellationToken);

        async Task<(ShowroomSaleItem? Sale, string? Error)> FailAfterClaimAsync(string error)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (null, error);
        }

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

        var qtyError = ShowroomApiRules.ValidateSaleQuantity(request.Quantity, product.Quantity);
        if (qtyError != null)
        {
            return await FailAfterClaimAsync(qtyError);
        }

        var (unitPrice, listPrice, campaignId, campaignPercent) = await PriceAsync(product, cancellationToken);
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

        var saleNote = string.IsNullOrWhiteSpace(note)
            ? $"From purchase request #{request.Id}"
            : note.Trim();
        var sale = new InPersonSaleReference
        {
            ProductId = request.ProductId,
            PurchaseRequestId = request.Id,
            Quantity = request.Quantity,
            UnitPrice = unitPrice,
            ListUnitPrice = listPrice,
            CampaignId = campaignId,
            CampaignDiscountPercent = campaignPercent,
            RecordedByUserId = recordedByUserId.Trim(),
            Note = saleNote,
            IdempotencyKey = completeKey,
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
            priorSale = await _db.InPersonSales.AsNoTracking()
                .Include(item => item.Product)
                .FirstOrDefaultAsync(item => item.IdempotencyKey == completeKey && item.VoidedAt == null, cancellationToken);
            return priorSale == null
                ? (null, "The sale could not be recorded.")
                : (MapSale(priorSale), null);
        }

        sale.Product = product;
        return (MapSale(sale), null);
    }

    public async Task<IReadOnlyList<ShowroomSaleItem>> ListSalesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.InPersonSales.AsNoTracking()
            .Where(sale => !sale.IsDemo && sale.VoidedAt == null)
            .Include(sale => sale.Product)
            .OrderByDescending(sale => sale.SoldAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        return rows.Select(MapSale).ToList();
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
                SET Status = {ShowroomApiRules.StatusContacted}, UpdatedAt = {voidedAt}
                WHERE Id = {requestId} AND Status = {ShowroomApiRules.StatusCompleted}
                """, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return (true, null);
    }

    private async Task<int> SumOpenReservedAsync(
        int productId,
        int? excludeRequestId,
        CancellationToken cancellationToken)
    {
        var query = _db.ShowroomPurchaseRequests.AsNoTracking()
            .Where(item => item.ProductId == productId
                && (item.Status == ShowroomApiRules.StatusNew || item.Status == ShowroomApiRules.StatusContacted));
        if (excludeRequestId is int excludeId)
        {
            query = query.Where(item => item.Id != excludeId);
        }

        return await query.SumAsync(item => (int?)item.Quantity, cancellationToken) ?? 0;
    }

    private async Task<(decimal UnitPrice, decimal ListPrice, int? CampaignId, decimal? CampaignPercent)> PriceAsync(
        ShopProductReference product,
        CancellationToken cancellationToken)
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
                .OrderBy(campaign => ShowroomApiRules.DiscountedPrice(
                    product.Price,
                    campaign.DiscountedPrice,
                    campaign.DiscountPercent))
                .ThenBy(campaign => campaign.Id)
                .FirstOrDefault();

        if (offer == null)
        {
            return (product.Price, product.Price, null, null);
        }

        var unit = ShowroomApiRules.DiscountedPrice(product.Price, offer.DiscountedPrice, offer.DiscountPercent);
        var percent = ShowroomApiRules.BadgePercent(product.Price, unit, offer.DiscountPercent);
        return (unit, product.Price, offer.Id, percent);
    }

    private static ShowroomRequestItem MapRequest(ShowroomPurchaseRequestReference request)
    {
        return new ShowroomRequestItem
        {
            Id = request.Id,
            ProductId = request.ProductId,
            ProductName = request.Product?.Name,
            StockNow = request.Product?.Quantity,
            CustomerName = request.CustomerName,
            Email = request.Email,
            Phone = request.Phone,
            Quantity = request.Quantity,
            PreferredVisitDate = request.PreferredVisitDate,
            Note = request.Note,
            Status = request.Status,
            HasLinkedAccount = !string.IsNullOrEmpty(request.UserId),
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            AllowedNextStatuses = ShowroomApiRules.AllStatuses
                .Where(status => ShowroomApiRules.CanTransition(request.Status, status))
                .ToList()
        };
    }

    private static ShowroomSaleItem MapSale(InPersonSaleReference sale)
    {
        return new ShowroomSaleItem
        {
            Id = sale.Id,
            ProductId = sale.ProductId,
            ProductName = sale.Product?.Name,
            Quantity = sale.Quantity,
            UnitPrice = sale.UnitPrice,
            ListUnitPrice = sale.ListUnitPrice,
            CampaignId = sale.CampaignId,
            CampaignDiscountPercent = sale.CampaignDiscountPercent,
            Note = sale.Note,
            SoldAt = sale.SoldAt,
            RecordedByUserId = sale.RecordedByUserId
        };
    }
}
