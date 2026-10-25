using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models.Api;

public sealed class PartAdminItemDto
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

public sealed class PartAdminListDto
{
    public List<PartAdminItemDto> Items { get; set; } = new();

    public List<string> Categories { get; set; } = new();

    public int Total { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}

public sealed class SavePartDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string Category { get; set; } = string.Empty;

    public string? Brand { get; set; }

    public string? Compatibility { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
    public int Quantity { get; set; }

    public string ImagePath { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public sealed class PartDeleteDto
{
    public string Outcome { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
