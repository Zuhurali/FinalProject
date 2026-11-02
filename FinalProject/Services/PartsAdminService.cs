using FinalProject.Commerce;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.Models.Entities;
using FinalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Services;

public sealed class PartsAdminService : IPartsAdminService
{
    public const int DefaultPageSize = 10;

    private readonly FinalProjectDbContext _db;

    public PartsAdminService(FinalProjectDbContext db)
    {
        _db = db;
    }

    public async Task<PartAdminListResponse> ListAsync(
        string? search,
        string? category,
        string? brand,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize is < 1 or > 50 ? DefaultPageSize : pageSize;
        var query = Filtered(search, category, brand);
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
        var ratings = await RatingsAsync(rows.Select(part => part.Id).ToList(), cancellationToken);
        return new PartAdminListResponse
        {
            Items = rows.Select(part => ToItem(part, ratings)).ToList(),
            Categories = await _db.ShopProducts.AsNoTracking()
                .Where(part => part.Category != "")
                .Select(part => part.Category)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync(cancellationToken),
            Brands = await _db.ShopProducts.AsNoTracking()
                .Where(part => part.Brand != "")
                .Select(part => part.Brand)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync(cancellationToken),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PartAdminItem?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var part = await _db.ShopProducts.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (part == null)
        {
            return null;
        }

        var ratings = await RatingsAsync([id], cancellationToken);
        return ToItem(part, ratings);
    }

    public async Task<(PartAdminItem? Part, string? Error)> CreateAsync(
        SavePartRequest request,
        CancellationToken cancellationToken = default)
    {
        var error = Validate(request);
        if (error != null)
        {
            return (null, error);
        }

        var part = new ShopProductReference
        {
            CreatedAt = DateTime.UtcNow
        };
        Apply(part, request);
        // New spare parts enter the public catalog as active (existing product policy).
        part.IsActive = true;
        _db.ShopProducts.Add(part);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return (null, "The part could not be saved. Check price, stock, and image path.");
        }

        return (ToItem(part, new Dictionary<int, (double Average, int Count)>()), null);
    }

    public async Task<(PartAdminItem? Part, string? Error)> UpdateAsync(
        int id,
        SavePartRequest request,
        CancellationToken cancellationToken = default)
    {
        var error = Validate(request);
        if (error != null)
        {
            return (null, error);
        }

        var part = await _db.ShopProducts.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (part == null)
        {
            return (null, "The part was not found.");
        }

        var expectedQuantity = part.Quantity;
        var newQuantity = request.Quantity;
        Apply(part, request);
        if (newQuantity != expectedQuantity)
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
                return (null, "Stock changed since this part was loaded (likely from a sale). Refresh and try again.");
            }

            // Keep tracked values for the response DTO without letting EF overwrite the claimed stock.
            // Setting IsModified=false after changing CurrentValue would revert to OriginalValue.
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
        }
        catch (DbUpdateException)
        {
            return (null, "The part could not be updated. Check price, stock, and image path.");
        }

        var ratings = await RatingsAsync([id], cancellationToken);
        return (ToItem(part, ratings), null);
    }

    public async Task<(bool Found, string Outcome, string Message)> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var part = await _db.ShopProducts.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (part == null)
        {
            return (false, "missing", "The part was not found.");
        }

        var dependents = await DependentCountAsync(id, cancellationToken);
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

    private IQueryable<ShopProductReference> Filtered(string? search, string? category, string? brand)
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

    private async Task<Dictionary<int, (double Average, int Count)>> RatingsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<int, (double Average, int Count)>();
        }

        var rows = await _db.ProductRatings.AsNoTracking()
            .Where(rating => ids.Contains(rating.ProductId))
            .GroupBy(rating => rating.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Count = group.Count(),
                Average = group.Average(item => (double)item.Rating)
            })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(row => row.ProductId, row => (row.Average, row.Count));
    }

    private async Task<int> DependentCountAsync(int id, CancellationToken cancellationToken)
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

    private static string? Validate(SavePartRequest request)
    {
        request.Name = (request.Name ?? string.Empty).Trim();
        request.Category = (request.Category ?? string.Empty).Trim();
        request.Brand = (request.Brand ?? string.Empty).Trim();
        request.PartNumber = (request.PartNumber ?? string.Empty).Trim();
        request.Description = (request.Description ?? string.Empty).Trim();
        request.Compatibility = (request.Compatibility ?? string.Empty).Trim();
        request.CarMake = (request.CarMake ?? string.Empty).Trim();
        request.CarModel = (request.CarModel ?? string.Empty).Trim();
        request.Color = (request.Color ?? string.Empty).Trim();
        request.ImagePath = (request.ImagePath ?? string.Empty).Trim();
        if (request.Name.Length is < 2 or > 200)
        {
            return "Enter a part name.";
        }

        if (request.Category.Length is < 2 or > 100)
        {
            return "Enter a category.";
        }

        if (request.PartNumber.Length > 80)
        {
            return "Part number / SKU cannot exceed 80 characters.";
        }

        if (request.CarMake.Length > 100 || request.CarModel.Length > 100)
        {
            return "Compatible vehicle fields are too long.";
        }

        if (request.Color.Length > 50)
        {
            return "Color cannot exceed 50 characters.";
        }

        if (request.Price < 0)
        {
            return "Price cannot be negative.";
        }

        if (request.Quantity < 0)
        {
            return "Stock cannot be negative.";
        }

        var imageError = PartImagePathRules.Validate(request.ImagePath);
        if (imageError != null)
        {
            return imageError;
        }

        return null;
    }

    private static void Apply(ShopProductReference part, SavePartRequest request)
    {
        part.Name = request.Name;
        part.Description = request.Description ?? string.Empty;
        part.Price = request.Price;
        part.Category = request.Category;
        part.Brand = request.Brand ?? string.Empty;
        part.PartNumber = request.PartNumber ?? string.Empty;
        part.Compatibility = request.Compatibility ?? string.Empty;
        part.CarMake = request.CarMake ?? string.Empty;
        part.CarModel = request.CarModel ?? string.Empty;
        part.Color = request.Color ?? string.Empty;
        part.Quantity = request.Quantity;
        part.ImagePath = request.ImagePath!.Trim();
        part.InStock = request.Quantity > 0;
        part.IsActive = request.IsActive;
        part.SearchKeywords = string.Join(" ", new[]
            {
                request.Name,
                request.Brand,
                request.PartNumber,
                request.Category,
                request.Compatibility,
                request.CarMake,
                request.CarModel
            }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        part.Fuel ??= string.Empty;
    }

    private static PartAdminItem ToItem(
        ShopProductReference part,
        IReadOnlyDictionary<int, (double Average, int Count)> ratings)
    {
        ratings.TryGetValue(part.Id, out var rating);
        return new PartAdminItem
        {
            Id = part.Id,
            Name = part.Name,
            Category = part.Category,
            Brand = part.Brand,
            PartNumber = part.PartNumber,
            Description = part.Description,
            Compatibility = part.Compatibility,
            CarMake = part.CarMake,
            CarModel = part.CarModel,
            Color = part.Color,
            Price = part.Price,
            Quantity = part.Quantity,
            ImagePath = part.ImagePath,
            IsActive = part.IsActive,
            Rating = rating.Count == 0 ? 0 : Math.Round(rating.Average, 1),
            RatingCount = rating.Count,
            CreatedAt = part.CreatedAt
        };
    }
}
