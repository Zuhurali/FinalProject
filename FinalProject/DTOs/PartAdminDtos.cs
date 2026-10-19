using System.ComponentModel.DataAnnotations;

namespace FinalProject.DTOs;

public sealed class PartAdminItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Compatibility { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public string ImagePath { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public double Rating { get; set; }

    public int RatingCount { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class PartAdminListResponse
{
    public List<PartAdminItem> Items { get; set; } = new();

    public List<string> Categories { get; set; } = new();

    public int Total { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}

public sealed class SavePartRequest
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 1000000)]
    public decimal Price { get; set; }

    [Required, MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Brand { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Compatibility { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [Required, MaxLength(500)]
    public string ImagePath { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public sealed class PartDeleteResponse
{
    public string Outcome { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
