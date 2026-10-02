namespace FinalProject.MVC.Models.Api;

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

    public string DriveType { get; set; } = string.Empty;

    public BrandApiDto? Brand { get; set; }

    public CategoryApiDto? Category { get; set; }

    public List<CarImageApiDto> Images { get; set; } = new();

    public string MainImage =>
        Images.OrderBy(image => image.DisplayOrder).FirstOrDefault(image => image.IsMain)?.ImageUrl
        ?? Images.OrderBy(image => image.DisplayOrder).FirstOrDefault()?.ImageUrl
        ?? string.Empty;
}
