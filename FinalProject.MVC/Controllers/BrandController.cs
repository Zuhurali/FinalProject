using FinalProject.MVC.Models;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Mappings;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

public class BrandController : Controller
{
    private static readonly Dictionary<string, string> LogoPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BMW"] = "~/assets/images/brand/04.svg",
        ["Mazda"] = "~/assets/images/brand/03.svg",
        ["Honda"] = "~/assets/images/brand/05.svg",
        ["Audi"] = "~/assets/images/brand/06.svg",
        ["Nissan"] = "~/assets/images/brand/07.svg",
        ["Jeep"] = "~/assets/images/brand/01.svg",
        ["Ford"] = "~/assets/images/brand/02.svg"
    };

    private readonly ICarApiService _carApi;

    public BrandController(ICarApiService carApi)
    {
        _carApi = carApi;
    }

    public async Task<IActionResult> Details(int id)
    {
        CarLookupDto lookups;
        try
        {
            lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted);
        }
        catch (CarApiException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var brand = lookups.Brands.FirstOrDefault(item => item.Id == id);
        if (brand == null)
        {
            return NotFound();
        }

        IReadOnlyList<Car> cars;
        try
        {
            var catalog = await _carApi.GetAllCarsAsync(new CarApiQuery { BrandId = brand.Id }, HttpContext.RequestAborted);
            cars = catalog.Select(CarApiMapper.ToCar).OrderBy(car => car.Name).ToList();
        }
        catch (CarApiException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        ViewData["Title"] = $"{brand.Name} - AutoVault";
        return View(new BrandDetailsViewModel
        {
            Brand = brand,
            LogoPath = LogoPaths.TryGetValue(brand.Name, out var logo) ? logo : string.Empty,
            Cars = cars
        });
    }
}
