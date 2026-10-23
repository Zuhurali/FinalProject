using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

public class AboutController : Controller
{
    private static readonly LuxuryShowcaseBrand[] ShowcaseTemplate =
    [
        new()
        {
            Name = "Lamborghini",
            Descriptor = "Italian performance icons",
            ImagePath = "~/assets/images/catalog/sports-car/huracan-1.jpg",
            HasListings = false
        },
        new()
        {
            Name = "Ferrari",
            Descriptor = "Racing-bred grand tourers",
            ImagePath = "~/assets/images/catalog/sports-car/f8-1.jpg",
            HasListings = false
        },
        new()
        {
            Name = "Bentley",
            Descriptor = "Handcrafted British luxury",
            ImagePath = "~/assets/images/catalog/luxury/continental-1.jpg",
            HasListings = false
        },
        new()
        {
            Name = "Rolls-Royce",
            Descriptor = "The pinnacle of comfort",
            ImagePath = "~/assets/images/catalog/luxury/rolls-royce-phantom-viii-front.jpg",
            HasListings = false
        }
    ];

    private readonly ICarApiService _carApi;
    private readonly ILogger<AboutController> _logger;

    public AboutController(ICarApiService carApi, ILogger<AboutController> logger)
    {
        _carApi = carApi;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "About Us - AutoVault";
        var availableMakes = await LoadAvailableMakesAsync(cancellationToken);
        var showcase = ShowcaseTemplate
            .Select(brand => new LuxuryShowcaseBrand
            {
                Name = brand.Name,
                Descriptor = brand.Descriptor,
                ImagePath = brand.ImagePath,
                HasListings = availableMakes.Contains(brand.Name)
            })
            .ToList();

        return View(showcase);
    }

    private async Task<HashSet<string>> LoadAvailableMakesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var cars = await _carApi.GetAllCarsAsync(cancellationToken: cancellationToken);
            return cars
                .Select(car => car.Brand?.Name?.Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase)!;
        }
        catch (CarApiException exception)
        {
            _logger.LogWarning(exception, "Luxury showcase could not load catalog brands.");
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
