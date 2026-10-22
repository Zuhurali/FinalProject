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
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize is < 1 or > 50 ? DefaultPageSize : pageSize;
        var query = Filtered(search, category);
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
        part.IsActive = true;
        _db.ShopProducts.Add(part);
        await _db.SaveChangesAsync(cancellationToken);
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

        Apply(part, request);
        await _db.SaveChangesAsync(cancellationToken);
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
            await _db.SaveChangesAsync(cancellationToken);
            return (true, "inactive", "This part has related records, so it was marked inactive instead of deleted.");
        }

        _db.ShopProducts.Remove(part);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, "deleted", "The part was deleted.");
    }

    private IQueryable<ShopProductReference> Filtered(string? search, string? category)
    {
        var query = _db.ShopProducts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(part =>
                part.Name.Contains(term) ||
                part.Brand.Contains(term) ||
                part.Category.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(part => part.Category == category);
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
                + (SELECT COUNT(*) FROM ProductCampaigns WHERE ProductId = {0}) AS Value
            """,
            id).ToListAsync(cancellationToken);
        return counts.FirstOrDefault();
    }

    private static string? Validate(SavePartRequest request)
    {
        request.Name = (request.Name ?? string.Empty).Trim();
        request.Category = (request.Category ?? string.Empty).Trim();
        request.Brand = (request.Brand ?? string.Empty).Trim();
        request.Description = (request.Description ?? string.Empty).Trim();
        request.Compatibility = (request.Compatibility ?? string.Empty).Trim();
        request.ImagePath = (request.ImagePath ?? string.Empty).Trim();
        if (request.Name.Length is < 2 or > 200)
        {
            return "Enter a part name.";
        }

        if (request.Category.Length is < 2 or > 100)
        {
            return "Enter a category.";
        }

        if (request.Price < 0)
        {
            return "Price cannot be negative.";
        }

        if (request.Quantity < 0)
        {
            return "Stock cannot be negative.";
        }

        if (string.IsNullOrWhiteSpace(request.ImagePath) || request.ImagePath.Length > 500)
        {
            return "Add a product image.";
        }

        return null;
    }

    private static void Apply(ShopProductReference part, SavePartRequest request)
    {
        part.Name = request.Name;
        part.Description = request.Description;
        part.Price = request.Price;
        part.Category = request.Category;
        part.Brand = request.Brand;
        part.Compatibility = request.Compatibility;
        part.Quantity = request.Quantity;
        part.ImagePath = request.ImagePath;
        part.InStock = request.Quantity > 0;
        part.IsActive = request.IsActive;
        part.SearchKeywords = string.Join(" ", new[] { request.Name, request.Brand, request.Category, request.Compatibility }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        if (part.Color == null)
        {
            part.Color = string.Empty;
        }

        if (part.Fuel == null)
        {
            part.Fuel = string.Empty;
        }

        if (part.CarMake == null)
        {
            part.CarMake = string.Empty;
        }

        if (part.CarModel == null)
        {
            part.CarModel = string.Empty;
        }
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
            Description = part.Description,
            Compatibility = part.Compatibility,
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
