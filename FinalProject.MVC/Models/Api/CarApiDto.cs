namespace FinalProject.MVC.Models.Api;

public class CarApiQuery
{
    public const int MaxPageSize = 100;

    public string? Search { get; set; }

    public int? BrandId { get; set; }

    public int? CategoryId { get; set; }

    public string? Model { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public int? MinYear { get; set; }

    public int? MaxYear { get; set; }

    public string? Fuel { get; set; }

    public string? Transmission { get; set; }

    public bool? IsNew { get; set; }

    public string? Status { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public class PagedCarsDto
{
    public List<CarApiDto> Items { get; set; } = new();

    public int CurrentPage { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }
}

public class BrandApiDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class CategoryApiDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class CarImageApiDto
{
    public int Id { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public bool IsMain { get; set; }

    public int DisplayOrder { get; set; }
}

public class CarApiDto
{
    public int Id { get; set; }

    public string Model { get; set; } = string.Empty;

    public int Year { get; set; }

    public decimal Price { get; set; }

    public int Mileage { get; set; }

    public string FuelType { get; set; } = string.Empty;

    public string Transmission { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsNew { get; set; }

    public bool IsFeatured { get; set; }

    public string? OwnerId { get; set; }

    public string? OwnerDisplayName { get; set; }

    public string Status { get; set; } = "Active";

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string DriveType { get; set; } = string.Empty;

    public BrandApiDto? Brand { get; set; }

    public CategoryApiDto? Category { get; set; }

    public List<CarImageApiDto> Images { get; set; } = new();

    public string MainImage =>
        Images?.OrderBy(image => image.DisplayOrder).FirstOrDefault(image => image.IsMain)?.ImageUrl
        ?? Images?.OrderBy(image => image.DisplayOrder).FirstOrDefault()?.ImageUrl
        ?? string.Empty;
}

public class CarSuggestionDto
{
    public string Text { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;
}
