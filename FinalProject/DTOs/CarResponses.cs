namespace FinalProject.DTOs;

public class BrandResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class CategoryResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class CarImageResponse
{
    public int Id { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public bool IsMain { get; set; }

    public int DisplayOrder { get; set; }
}

public class CarResponse
{
    public int Id { get; set; }

    public string Model { get; set; } = string.Empty;

    public int Year { get; set; }

    public decimal Price { get; set; }

    public int Mileage { get; set; }

    public string FuelType { get; set; } = string.Empty;

    public string Transmission { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public int? Seats { get; set; }

    public string? DriveType { get; set; }

    public string? Engine { get; set; }

    public string? MileageUnit { get; set; }

    public int? Horsepower { get; set; }

    public int? Doors { get; set; }

    public int? Cylinders { get; set; }

    public string? Steering { get; set; }

    public string? Vin { get; set; }

    public string? AccidentHistory { get; set; }

    public string? RepaintedParts { get; set; }

    public string? ReplacedParts { get; set; }

    public string? CustomsStatus { get; set; }

    public string? MarketOrigin { get; set; }

    public List<string> Features { get; set; } = new();

    public string Description { get; set; } = string.Empty;

    public bool IsNew { get; set; }

    public bool IsFeatured { get; set; }

    public string? OwnerId { get; set; }

    public string? OwnerDisplayName { get; set; }

    public string Status { get; set; } = "Active";

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public BrandResponse Brand { get; set; } = null!;

    public CategoryResponse Category { get; set; } = null!;

    public List<CarImageResponse> Images { get; set; } = new();
}
