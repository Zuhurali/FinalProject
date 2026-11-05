using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models;
using FinalProject.MVC.DTOs;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Mappings;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

public class BrandController : Controller
{
    private readonly ICarApiService _carApi;
    private readonly IPartnerStripService _partners;

    public BrandController(ICarApiService carApi, IPartnerStripService partners)
    {
        _carApi = carApi;
        _partners = partners;
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
            var catalog = await _carApi.GetAllCarsAsync(
                CarPublicListing.WithActiveStatus(new CarApiQueryDto { BrandId = brand.Id }),
                HttpContext.RequestAborted);
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
            LogoPath = _partners.FindLogoPathByName(brand.Name) ?? string.Empty,
            Cars = cars
        });
    }
}
