using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

public class AboutController : Controller
{
    private readonly IAboutPageContentService _aboutPage;
    private readonly ICarApiService _carApi;
    private readonly ILogger<AboutController> _logger;

    public AboutController(
        IAboutPageContentService aboutPage,
        ICarApiService carApi,
        ILogger<AboutController> logger)
    {
        _aboutPage = aboutPage;
        _carApi = carApi;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var content = _aboutPage.GetPublic();
        ViewData["Title"] = string.IsNullOrWhiteSpace(content.SeoTitle) ? content.PageTitle : content.SeoTitle;
        ViewData["MetaDescription"] = content.SeoDescription;

        var availableMakes = await LoadAvailableMakesAsync(cancellationToken);
        var showcase = BuildShowcase(content, availableMakes);

        return View(new AboutPageViewModel
        {
            Content = content,
            Showcase = showcase
        });
    }

    private static IReadOnlyList<LuxuryShowcaseBrand> BuildShowcase(
        AboutPagePublicContent content,
        HashSet<string> availableMakes)
    {
        var templates = new (string Name, string Descriptor, string ImagePath)[]
        {
            (content.Showcase1Name, content.Showcase1Descriptor, content.Showcase1ImagePath),
            (content.Showcase2Name, content.Showcase2Descriptor, content.Showcase2ImagePath),
            (content.Showcase3Name, content.Showcase3Descriptor, content.Showcase3ImagePath),
            (content.Showcase4Name, content.Showcase4Descriptor, content.Showcase4ImagePath)
        };

        return templates
            .Select(brand => new LuxuryShowcaseBrand
            {
                Name = brand.Name,
                Descriptor = brand.Descriptor,
                ImagePath = brand.ImagePath,
                HasListings = availableMakes.Contains(brand.Name)
            })
            .ToList();
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
