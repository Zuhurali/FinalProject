using FinalProject.Commerce;
using FinalProject.Repositories;
using FinalProject.DTOs;
using FinalProject.Models.Entities;
using FinalProject.Services.Interfaces;

namespace FinalProject.Services;

public sealed class PartsAdminService : IPartsAdminService
{
    public const int DefaultPageSize = 10;

    private readonly IPartsRepository _db;

    public PartsAdminService(IPartsRepository db)
    {
        _db = db;
    }

    public async Task<PartAdminListResponseDto> ListAsync(
        string? search,
        string? category,
        string? brand,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize is < 1 or > 50 ? DefaultPageSize : pageSize;
        var result = await _db.ListPartsAsync(search, category, brand, page, pageSize, cancellationToken);
        var ratings = await RatingsAsync(result.Items.Select(part => part.Id).ToList(), cancellationToken);
        return new PartAdminListResponseDto
        {
            Items = result.Items.Select(part => ToItem(part, ratings)).ToList(),
            Categories = result.Categories.ToList(),
            Brands = result.Brands.ToList(),
            Total = result.Total,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    public async Task<PartAdminItemDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var part = await _db.GetPartByIdNoTrackingAsync(id, cancellationToken);
        if (part == null)
        {
            return null;
        }

        var ratings = await RatingsAsync([id], cancellationToken);
        return ToItem(part, ratings);
    }

    public async Task<(PartAdminItemDto? Part, string? Error)> CreateAsync(
        SavePartRequestDto request,
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
        var created = await _db.AddPartAsync(part, cancellationToken);
        if (created.Error != null)
        {
            return (null, created.Error);
        }

        return (ToItem(created.Part!, new Dictionary<int, (double Average, int Count)>()), null);
    }

    public async Task<(PartAdminItemDto? Part, string? Error)> UpdateAsync(
        int id,
        SavePartRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var error = Validate(request);
        if (error != null)
        {
            return (null, error);
        }

        var part = await _db.GetPartByIdTrackedAsync(id, cancellationToken);
        if (part == null)
        {
            return (null, "The part was not found.");
        }

        var expectedQuantity = part.Quantity;
        var newQuantity = request.Quantity;
        Apply(part, request);
        var quantityChanged = newQuantity != expectedQuantity;
        var save = await _db.SavePartUpdateAsync(part, expectedQuantity, newQuantity, quantityChanged, cancellationToken);
        if (save.StockConcurrencyFailed)
        {
            return (null, "Stock changed since this part was loaded (likely from a sale). Refresh and try again.");
        }

        if (save.SaveError != null)
        {
            return (null, save.SaveError);
        }

        var ratings = await RatingsAsync([id], cancellationToken);
        return (ToItem(part, ratings), null);
    }

    public Task<(bool Found, string Outcome, string Message)> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        _db.DeletePartAsync(id, cancellationToken);

    private async Task<Dictionary<int, (double Average, int Count)>> RatingsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        var rows = await _db.GetRatingAggregatesAsync(ids, cancellationToken);
        return rows.ToDictionary(row => row.ProductId, row => (row.Average, row.Count));
    }

    private static string? Validate(SavePartRequestDto request)
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

    private static void Apply(ShopProductReference part, SavePartRequestDto request)
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

    private static PartAdminItemDto ToItem(
        ShopProductReference part,
        IReadOnlyDictionary<int, (double Average, int Count)> ratings)
    {
        ratings.TryGetValue(part.Id, out var rating);
        return new PartAdminItemDto
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
