using FinalProject.MVC.DTOs;

namespace FinalProject.MVC.Models;

public sealed class BrandDetailsViewModel
{
    public BrandApiDto Brand { get; set; } = new();

    public string LogoPath { get; set; } = string.Empty;

    public IReadOnlyList<Car> Cars { get; set; } = Array.Empty<Car>();
}
