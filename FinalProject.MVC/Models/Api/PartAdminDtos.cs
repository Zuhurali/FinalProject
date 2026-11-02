using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models.Api;

public sealed class PartAdminItemDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;

    public string PartNumber { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Compatibility { get; set; } = string.Empty;

    public string CarMake { get; set; } = string.Empty;

    public string CarModel { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public string ImagePath { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public double Rating { get; set; }

    public int RatingCount { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class PartAdminListDto
{
    public List<PartAdminItemDto> Items { get; set; } = new();

    public List<string> Categories { get; set; } = new();

    public List<string> Brands { get; set; } = new();

    public int Total { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}

public sealed class SavePartDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Enter a part name.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Enter a part name.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(0, 1000000, ErrorMessage = "Price cannot be negative.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Enter a category.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Enter a category.")]
    public string Category { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Brand { get; set; }

    [Display(Name = "Part number / SKU")]
    [StringLength(80)]
    public string? PartNumber { get; set; }

    [StringLength(300)]
    public string? Compatibility { get; set; }

    [Display(Name = "Compatible car make")]
    [StringLength(100)]
    public string? CarMake { get; set; }

    [Display(Name = "Compatible car model")]
    [StringLength(100)]
    public string? CarModel { get; set; }

    [Display(Name = "Color")]
    [StringLength(50, ErrorMessage = "Color cannot exceed 50 characters.")]
    public string? Color { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
    public int Quantity { get; set; }

    /// <summary>
    /// Stored relative image path. Nullable so MVC does not treat an empty create form as
    /// "ImagePath is required" before the uploaded file is saved into this property.
    /// </summary>
    [Display(Name = "Product image")]
    [StringLength(500)]
    public string? ImagePath { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class PartDeleteDto
{
    public string Outcome { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
