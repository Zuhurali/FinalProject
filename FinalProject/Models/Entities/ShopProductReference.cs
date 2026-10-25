namespace FinalProject.Models.Entities;

public class ShopProductReference
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public string Brand { get; set; } = string.Empty;

    public string PartNumber { get; set; } = string.Empty;

    public string Compatibility { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string CarMake { get; set; } = string.Empty;

    public string CarModel { get; set; } = string.Empty;

    public string ImagePath { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool InStock { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Color { get; set; } = string.Empty;

    public string Fuel { get; set; } = string.Empty;

    public string SearchKeywords { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
