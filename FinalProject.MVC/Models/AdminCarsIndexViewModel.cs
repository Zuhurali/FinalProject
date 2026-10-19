using FinalProject.MVC.Models.Api;

namespace FinalProject.MVC.Models;

public sealed class AdminCarsIndexViewModel
{
    public IReadOnlyList<CarApiDto> Items { get; set; } = Array.Empty<CarApiDto>();

    public IReadOnlyList<BrandApiDto> Brands { get; set; } = Array.Empty<BrandApiDto>();

    public string? Search { get; set; }

    public int? BrandId { get; set; }

    public string? Status { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }
}
