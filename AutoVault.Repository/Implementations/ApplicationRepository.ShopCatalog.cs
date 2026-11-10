using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Repositories.Implementations;

public sealed partial class ApplicationRepository
{
    public async Task<IReadOnlyList<ShopCatalogProduct>> GetActiveShopProductsOrderedAsync(
        CancellationToken cancellationToken = default) =>
        await _db.ShopProducts.AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.Id)
            .ToListAsync(cancellationToken);

    public Task<ShopCatalogProduct?> GetShopProductByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.ShopProducts.AsNoTracking().FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

    public void AddShopProduct(ShopCatalogProduct product) => _db.ShopProducts.Add(product);

    public Task<ShopCatalogProduct?> FindTrackedShopProductAsync(int id, CancellationToken cancellationToken = default) =>
        _db.ShopProducts.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<int> TryUpdateShopProductQuantityAsync(
        int productId,
        int expectedQuantity,
        int newQuantity,
        ShopCatalogProduct tracked,
        CancellationToken cancellationToken = default)
    {
        var affected = await _db.ShopProducts
            .Where(item => item.Id == productId && item.Quantity == expectedQuantity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.Quantity, newQuantity)
                    .SetProperty(item => item.InStock, newQuantity > 0),
                cancellationToken);
        if (affected == 1)
        {
            _db.Entry(tracked).Property(item => item.Quantity).OriginalValue = newQuantity;
            _db.Entry(tracked).Property(item => item.Quantity).IsModified = false;
            _db.Entry(tracked).Property(item => item.InStock).OriginalValue = newQuantity > 0;
            _db.Entry(tracked).Property(item => item.InStock).IsModified = false;
            tracked.Quantity = newQuantity;
            tracked.InStock = newQuantity > 0;
        }

        return affected;
    }

    public void RemoveShopProduct(ShopCatalogProduct product) => _db.ShopProducts.Remove(product);

    public async Task<int> CountShopProductDependentsAsync(int productId, CancellationToken cancellationToken = default)
    {
        var campaigns = await _db.ProductCampaigns.CountAsync(item => item.ProductId == productId, cancellationToken);
        var sales = await _db.InPersonSales.CountAsync(item => item.ProductId == productId, cancellationToken);
        var inquiries = await _db.ProductInquiries.CountAsync(item => item.ProductId == productId, cancellationToken);
        var requests = await _db.ShowroomPurchaseRequests.CountAsync(item => item.ProductId == productId, cancellationToken);
        var feedback = await _db.Database.SqlQueryRaw<int>(
            """
            SELECT
                (SELECT COUNT(*) FROM ProductRatings WHERE ProductId = {0})
                + (SELECT COUNT(*) FROM ProductReviews WHERE ProductId = {0})
                + (SELECT COUNT(*) FROM ProductFavorites WHERE ProductId = {0}) AS Value
            """,
            productId).ToListAsync(cancellationToken);
        return campaigns + sales + inquiries + requests + feedback.FirstOrDefault();
    }

    public Task<int> CountShopProductsAsync(CancellationToken cancellationToken = default) =>
        _db.ShopProducts.CountAsync(cancellationToken);

    public Task<bool> AnyShopProductsAsync(CancellationToken cancellationToken = default) =>
        _db.ShopProducts.AnyAsync(cancellationToken);

    public async Task SeedShopProductsWithIdentityInsertAsync(
        IReadOnlyList<ShopCatalogProduct> templates,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await _db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT ShopProducts ON", cancellationToken);
        _db.ShopProducts.AddRange(templates);
        await SaveChangesAsync(cancellationToken);
        await _db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT ShopProducts OFF", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductCampaign>> GetAllCampaignsWithProductAsync(
        CancellationToken cancellationToken = default) =>
        await _db.ProductCampaigns.AsNoTracking()
            .Include(campaign => campaign.Product)
            .OrderByDescending(campaign => campaign.StartsAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductCampaign>> GetLiveCampaignsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default) =>
        await _db.ProductCampaigns.AsNoTracking()
            .Include(campaign => campaign.Product)
            .Where(campaign => campaign.IsActive && campaign.StartsAt <= utcNow && campaign.EndsAt > utcNow)
            .OrderByDescending(campaign => campaign.StartsAt)
            .ThenBy(campaign => campaign.Id)
            .ToListAsync(cancellationToken);

    public Task<ProductCampaign?> GetCampaignByIdWithProductAsync(int id, CancellationToken cancellationToken = default) =>
        _db.ProductCampaigns.AsNoTracking()
            .Include(campaign => campaign.Product)
            .FirstOrDefaultAsync(campaign => campaign.Id == id, cancellationToken);

    public void AddProductCampaign(ProductCampaign campaign) => _db.ProductCampaigns.Add(campaign);

    public Task<ProductCampaign?> FindTrackedProductCampaignAsync(int id, CancellationToken cancellationToken = default) =>
        _db.ProductCampaigns.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public void RemoveProductCampaign(ProductCampaign campaign) => _db.ProductCampaigns.Remove(campaign);

    public async Task<IReadOnlyList<ProductCampaign>> GetOtherActiveCampaignsWithProductAsync(
        int exceptCampaignId,
        CancellationToken cancellationToken = default) =>
        await _db.ProductCampaigns.AsNoTracking()
            .Include(item => item.Product)
            .Where(item => item.IsActive && item.Id != exceptCampaignId)
            .ToListAsync(cancellationToken);

    public Task<ShopCatalogProduct?> GetActiveShopProductByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.ShopProducts.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && item.IsActive, cancellationToken);

    public async Task<IReadOnlyList<InPersonSale>> GetRecordedSalesWithDetailsAsync(
        CancellationToken cancellationToken = default) =>
        await _db.InPersonSales.AsNoTracking()
            .Where(sale => !sale.IsDemo && sale.VoidedAt == null)
            .Include(sale => sale.Product)
            .Include(sale => sale.Campaign)
            .OrderByDescending(sale => sale.SoldAt)
            .Take(200)
            .ToListAsync(cancellationToken);

    public Task<InPersonSale?> GetSaleByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        _db.InPersonSales.AsNoTracking()
            .Include(sale => sale.Product)
            .Include(sale => sale.Campaign)
            .FirstOrDefaultAsync(sale => sale.Id == id, cancellationToken);

    public Task<InPersonSale?> FindSaleByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        _db.InPersonSales.AsNoTracking()
            .FirstOrDefaultAsync(
                sale => sale.IdempotencyKey == idempotencyKey && sale.VoidedAt == null,
                cancellationToken);

    public Task<int> DecrementShopProductStockAsync(int productId, int quantity, CancellationToken cancellationToken = default) =>
        _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE ShopProducts
            SET Quantity = Quantity - {quantity},
                InStock = CASE WHEN Quantity - {quantity} > 0 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
            WHERE Id = {productId} AND Quantity >= {quantity}
            """, cancellationToken);

    public void AddInPersonSale(InPersonSale sale) => _db.InPersonSales.Add(sale);

    public Task<int> ClaimVoidInPersonSaleAsync(
        int saleId,
        DateTime voidedAt,
        string voidedBy,
        CancellationToken cancellationToken = default) =>
        _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE InPersonSales
            SET VoidedAt = {voidedAt}, VoidedByUserId = {voidedBy}
            WHERE Id = {saleId} AND IsDemo = 0 AND VoidedAt IS NULL
            """, cancellationToken);

    public Task<InPersonSale> GetSaleByIdRequiredAsync(int saleId, CancellationToken cancellationToken = default) =>
        _db.InPersonSales.AsNoTracking().FirstAsync(item => item.Id == saleId, cancellationToken);

    public Task<int> IncrementShopProductStockAsync(int productId, int quantity, CancellationToken cancellationToken = default) =>
        _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE ShopProducts
            SET Quantity = Quantity + {quantity},
                InStock = CASE WHEN Quantity + {quantity} > 0 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
            WHERE Id = {productId}
            """, cancellationToken);

    public Task ReopenShowroomRequestAfterVoidAsync(
        int requestId,
        DateTime updatedAt,
        CancellationToken cancellationToken = default) =>
        _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE ShowroomPurchaseRequests
            SET Status = {ShowroomRequestStatus.Contacted}, UpdatedAt = {updatedAt}
            WHERE Id = {requestId} AND Status = {ShowroomRequestStatus.Completed}
            """, cancellationToken);

    public async Task<IReadOnlyList<ProductInquiry>> GetProductInquiriesAsync(
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ProductInquiries.AsNoTracking()
            .Include(inquiry => inquiry.Product)
            .AsQueryable();
        query = status?.Trim().ToLowerInvariant() switch
        {
            "new" or "awaiting reply" => query.Where(inquiry =>
                !inquiry.IsHandled && (inquiry.AdminReply == null || inquiry.AdminReply == "")),
            "answered" => query.Where(inquiry =>
                !inquiry.IsHandled
                && inquiry.AdminReply != null
                && inquiry.AdminReply != ""
                && (inquiry.UpdatedAt == null || inquiry.AdminRepliedAt == null || inquiry.UpdatedAt <= inquiry.AdminRepliedAt)),
            "reopened" or "awaiting follow-up" => query.Where(inquiry =>
                !inquiry.IsHandled
                && inquiry.AdminReply != null
                && inquiry.AdminReply != ""
                && inquiry.UpdatedAt != null
                && inquiry.AdminRepliedAt != null
                && inquiry.UpdatedAt > inquiry.AdminRepliedAt),
            "closed" => query.Where(inquiry => inquiry.IsHandled),
            _ => query
        };
        return await query.OrderByDescending(inquiry => inquiry.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductInquiry>> GetAnsweredProductInquiriesAsync(
        int productId,
        CancellationToken cancellationToken = default) =>
        await _db.ProductInquiries.AsNoTracking()
            .Where(inquiry =>
                inquiry.ProductId == productId
                && inquiry.AdminReply != null
                && inquiry.AdminReply != "")
            .OrderByDescending(inquiry => inquiry.CreatedAt)
            .ThenByDescending(inquiry => inquiry.Id)
            .ToListAsync(cancellationToken);

    public Task<ProductInquiry?> GetProductInquiryByIdWithProductAsync(int id, CancellationToken cancellationToken = default) =>
        _db.ProductInquiries.AsNoTracking()
            .Include(inquiry => inquiry.Product)
            .FirstOrDefaultAsync(inquiry => inquiry.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ProductInquiry>> GetProductInquiriesForUserAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        await _db.ProductInquiries.AsNoTracking()
            .Include(inquiry => inquiry.Product)
            .Where(inquiry => inquiry.UserId == userId)
            .OrderByDescending(inquiry => inquiry.CreatedAt)
            .ThenByDescending(inquiry => inquiry.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductInquiry>> FindOrphanedProductInquiriesByEmailAsync(
        string targetEmail,
        CancellationToken cancellationToken = default) =>
        await _db.ProductInquiries
            .Where(inquiry =>
                inquiry.UserId == null
                && inquiry.Email != null
                && inquiry.Email.ToLower() == targetEmail)
            .ToListAsync(cancellationToken);

    public Task<bool> AnyActiveShopProductAsync(int productId, CancellationToken cancellationToken = default) =>
        _db.ShopProducts.AsNoTracking()
            .AnyAsync(product => product.Id == productId && product.IsActive, cancellationToken);

    public void AddProductInquiry(ProductInquiry inquiry) => _db.ProductInquiries.Add(inquiry);

    public Task<ProductInquiry?> FindTrackedProductInquiryAsync(int id, CancellationToken cancellationToken = default) =>
        _db.ProductInquiries.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public void RemoveProductInquiry(ProductInquiry inquiry) => _db.ProductInquiries.Remove(inquiry);

    public async Task TryInsertInquiryReplyNotificationAsync(
        string userId,
        int inquiryId,
        string dedupKey,
        string body,
        DateTime createdAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($"""
                IF NOT EXISTS (
                    SELECT 1 FROM UserNotifications
                    WHERE UserId = {userId} AND DedupKey = {dedupKey})
                INSERT INTO UserNotifications
                    (UserId, [Type], Title, Body, Url, DedupKey, IsRead, CreatedAt)
                VALUES
                    ({userId}, N'inquiry_reply', N'AutoVault Support replied',
                     {body}, N'/Account/ProductQuestions', {dedupKey}, CAST(0 AS bit), {createdAt})
                """, cancellationToken);
        }
        catch (Exception)
        {
            // Best-effort notification insert.
        }
    }

    public async Task<IReadOnlyList<ShowroomPurchaseRequest>> GetShowroomPurchaseRequestsAsync(
        string? status,
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

    public Task<ShowroomPurchaseRequest?> GetShowroomPurchaseRequestByIdWithProductAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        _db.ShowroomPurchaseRequests.AsNoTracking()
            .Include(request => request.Product)
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ShowroomPurchaseRequest>> GetShowroomPurchaseRequestsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        await _db.ShowroomPurchaseRequests.AsNoTracking()
            .Include(request => request.Product)
            .Where(request => request.UserId == userId)
            .OrderByDescending(request => request.CreatedAt)
            .ThenByDescending(request => request.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ShowroomPurchaseRequest>> FindOrphanedShowroomPurchaseRequestsAsync(
        CancellationToken cancellationToken = default) =>
        await _db.ShowroomPurchaseRequests
            .Where(request => request.UserId == null)
            .ToListAsync(cancellationToken);

    public Task<ShowroomPurchaseRequest?> FindShowroomPurchaseRequestBySubmissionKeyAsync(
        Guid submissionKey,
        CancellationToken cancellationToken = default) =>
        _db.ShowroomPurchaseRequests.AsNoTracking()
            .FirstOrDefaultAsync(item => item.SubmissionKey == submissionKey, cancellationToken);

    public Task<ShowroomPurchaseRequest?> FindOpenShowroomPurchaseRequestAsync(
        int productId,
        string email,
        CancellationToken cancellationToken = default) =>
        _db.ShowroomPurchaseRequests.AsNoTracking()
            .Where(item => item.ProductId == productId &&
                           item.Email.ToLower() == email &&
                           (item.Status == ShowroomRequestStatus.New || item.Status == ShowroomRequestStatus.Contacted))
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<int> SumOpenReservedQuantityAsync(
        int productId,
        int? excludeRequestId,
        CancellationToken cancellationToken = default)
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

    public void AddShowroomPurchaseRequest(ShowroomPurchaseRequest request) => _db.ShowroomPurchaseRequests.Add(request);

    public Task<ShowroomPurchaseRequest?> FindTrackedShowroomPurchaseRequestAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        _db.ShowroomPurchaseRequests.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public void RemoveShowroomPurchaseRequest(ShowroomPurchaseRequest request) =>
        _db.ShowroomPurchaseRequests.Remove(request);

    public Task<int> ClaimShowroomPurchaseRequestForCompletionAsync(
        int requestId,
        DateTime completedAt,
        CancellationToken cancellationToken = default)
    {
        var openNew = ShowroomRequestStatus.New;
        var openContacted = ShowroomRequestStatus.Contacted;
        return _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE ShowroomPurchaseRequests
            SET Status = {ShowroomRequestStatus.Completed}, UpdatedAt = {completedAt}
            WHERE Id = {requestId}
              AND (Status = {openNew} OR Status = {openContacted})
            """, cancellationToken);
    }

    public Task<ShowroomPurchaseRequest> GetShowroomPurchaseRequestByIdRequiredAsync(
        int requestId,
        CancellationToken cancellationToken = default) =>
        _db.ShowroomPurchaseRequests.AsNoTracking().FirstAsync(item => item.Id == requestId, cancellationToken);
}
