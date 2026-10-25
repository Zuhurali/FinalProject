using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public class ShopCatalogProduct
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [MaxLength(100)]
    public string Brand { get; set; } = string.Empty;

    [MaxLength(80)]
    public string PartNumber { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Compatibility { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(100)]
    public string CarMake { get; set; } = string.Empty;

    [MaxLength(100)]
    public string CarModel { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string ImagePath { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public bool InStock { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    [MaxLength(50)]
    public string Color { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Fuel { get; set; } = string.Empty;

    [MaxLength(500)]
    public string SearchKeywords { get; set; } = string.Empty;
}
