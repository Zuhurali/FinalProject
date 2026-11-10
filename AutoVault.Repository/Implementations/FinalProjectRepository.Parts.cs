using FinalProject.Models.Entities;
using FinalProject.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Repositories.Implementations;

public sealed partial class FinalProjectRepository
{
    public async Task<PartAdminListPage> ListPartsAsync(
        string? search,
        string? category,
        string? brand,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = FilteredParts(search, category, brand);
        var total = await query.CountAsync(cancellationToken);
        var lastPage = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > lastPage)
        {
            page = lastPage;
        }

        var rows = await query
            .OrderBy(part => part.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var categories = await _db.ShopProducts.AsNoTracking()
            .Where(part => part.Category != "")
            .Select(part => part.Category)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);

        var brands = await _db.ShopProducts.AsNoTracking()
            .Where(part => part.Brand != "")
            .Select(part => part.Brand)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);

        return new PartAdminListPage(rows, categories, brands, total, page, pageSize);
    }

    public Task<ShopProductReference?> GetPartByIdNoTrackingAsync(int id, CancellationToken cancellationToken = default) =>
        _db.ShopProducts.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<ShopProductReference?> GetPartByIdTrackedAsync(int id, CancellationToken cancellationToken = default) =>
        _db.ShopProducts.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<(ShopProductReference? Part, string? Error)> AddPartAsync(
        ShopProductReference part,
        CancellationToken cancellationToken = default)
    {
        _db.ShopProducts.Add(part);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return (part, null);
        }
        catch (DbUpdateException)
        {
            return (null, "The part could not be saved. Check price, stock, and image path.");
        }
    }

    public async Task<(bool StockConcurrencyFailed, string? SaveError)> SavePartUpdateAsync(
        ShopProductReference part,
        int expectedQuantity,
        int newQuantity,
        bool quantityChanged,
        CancellationToken cancellationToken = default)
    {
        if (quantityChanged)
        {
            var affected = await _db.ShopProducts
                .Where(item => item.Id == part.Id && item.Quantity == expectedQuantity)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(item => item.Quantity, newQuantity)
                        .SetProperty(item => item.InStock, newQuantity > 0),
                    cancellationToken);
            if (affected != 1)
            {
                return (true, null);
            }

            _db.Entry(part).Property(item => item.Quantity).OriginalValue = newQuantity;
            _db.Entry(part).Property(item => item.Quantity).IsModified = false;
            _db.Entry(part).Property(item => item.InStock).OriginalValue = newQuantity > 0;
            _db.Entry(part).Property(item => item.InStock).IsModified = false;
            part.Quantity = newQuantity;
            part.InStock = newQuantity > 0;
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return (false, null);
        }
        catch (DbUpdateException)
        {
            return (false, "The part could not be updated. Check price, stock, and image path.");
        }
    }

    public async Task<(bool Found, string Outcome, string Message)> DeletePartAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var part = await _db.ShopProducts.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (part == null)
        {
            return (false, "missing", "The part was not found.");
        }

        var dependents = await GetPartDependentCountAsync(id, cancellationToken);
        if (dependents > 0)
        {
            part.IsActive = false;
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return (true, "error", "The part could not be marked inactive.");
            }

            return (true, "inactive", "This part has related records, so it was marked inactive instead of deleted.");
        }

        _db.ShopProducts.Remove(part);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return (true, "error", "The part could not be deleted.");
        }

        return (true, "deleted", "The part was deleted.");
    }

    public async Task<IReadOnlyList<PartRatingAggregateRow>> GetRatingAggregatesAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return Array.Empty<PartRatingAggregateRow>();
        }

        return await _db.ProductRatings.AsNoTracking()
            .Where(rating => productIds.Contains(rating.ProductId))
            .GroupBy(rating => rating.ProductId)
            .Select(group => new PartRatingAggregateRow(
                group.Key,
                group.Average(item => (double)item.Rating),
                group.Count()))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<ShopProductReference> FilteredParts(string? search, string? category, string? brand)
    {
        var query = _db.ShopProducts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(part =>
                part.Name.Contains(term) ||
                part.Brand.Contains(term) ||
                part.Category.Contains(term) ||
                part.PartNumber.Contains(term) ||
                part.CarMake.Contains(term) ||
                part.CarModel.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(part => part.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(brand))
        {
            query = query.Where(part => part.Brand == brand);
        }

        return query;
    }

    private async Task<int> GetPartDependentCountAsync(int id, CancellationToken cancellationToken)
    {
        var counts = await _db.Database.SqlQueryRaw<int>(
            """
            SELECT
                (SELECT COUNT(*) FROM ProductRatings WHERE ProductId = {0})
                + (SELECT COUNT(*) FROM ProductReviews WHERE ProductId = {0})
                + (SELECT COUNT(*) FROM ProductInquiries WHERE ProductId = {0})
                + (SELECT COUNT(*) FROM InPersonSales WHERE ProductId = {0})
                + (SELECT COUNT(*) FROM ShowroomPurchaseRequests WHERE ProductId = {0})
                + (SELECT COUNT(*) FROM ProductCampaigns WHERE ProductId = {0})
                + (SELECT COUNT(*) FROM ProductFavorites WHERE ProductId = {0}) AS Value
            """,
            id).ToListAsync(cancellationToken);
        return counts.FirstOrDefault();
    }
}
