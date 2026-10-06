using FinalProject.MVC.Models;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Cars")]
public sealed class AdminCarsController : AdminControllerBase
{
    private readonly ICarApiService _cars;

    public AdminCarsController(
        UserManager<ApplicationUser> userManager,
        IApiAccessTokenService tokenService,
        ICarApiService carApi)
        : base(userManager, tokenService)
    {
        _cars = carApi;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Admin Cars - AutoVault";
        try
        {
            return View(await _cars.GetAllCarsAsync(cancellationToken: cancellationToken));
        }
        catch (CarApiException)
        {
            ViewData["ServiceUnavailable"] = true;
            return View(Array.Empty<CarApiDto>());
        }
    }

    [Authorize(Roles = AppRoles.SuperAdmin)]
    [HttpGet("Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Car - AutoVault";
        return View(await EditorAsync(null, cancellationToken));
    }

    [Authorize(Roles = AppRoles.SuperAdmin)]
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CarEditorViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Lookups = await SafeLookupsAsync(cancellationToken);
            return View(model);
        }

        var token = await CreateTokenAsync();
        if (token == null)
        {
            return Challenge();
        }

        try
        {
            await _cars.CreateCatalogCarAsync(model.Car, token, cancellationToken);
            TempData["Success"] = "The car was created.";
            return RedirectToAction(nameof(Index));
        }
        catch (CarApiException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            model.Lookups = await SafeLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Car - AutoVault";
        var editor = await EditorAsync(id, cancellationToken);
        return editor == null ? NotFound() : View(editor);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CarEditorViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Id = id;
            model.Lookups = await SafeLookupsAsync(cancellationToken);
            return View(model);
        }

        var token = await CreateTokenAsync();
        if (token == null)
        {
            return Challenge();
        }

        try
        {
            await _cars.UpdateCarAsync(id, model.Car, token, cancellationToken);
            TempData["Success"] = "The car was updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (CarApiException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            model.Id = id;
            model.Lookups = await SafeLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var token = await CreateTokenAsync();
        if (token == null)
        {
            return Challenge();
        }

        try
        {
            await _cars.DeleteCarAsync(id, token, cancellationToken);
            TempData["Success"] = "The car was deleted.";
        }
        catch (CarApiException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<CarEditorViewModel?> EditorAsync(int? id, CancellationToken cancellationToken)
    {
        var lookups = await SafeLookupsAsync(cancellationToken);
        if (id == null)
        {
            return new CarEditorViewModel { Lookups = lookups };
        }

        var car = await _cars.GetCarByIdAsync(id.Value, cancellationToken);
        if (car == null)
        {
            return null;
        }

        return new CarEditorViewModel
        {
            Id = car.Id,
            Lookups = lookups,
            Car = new SaveCarDto
            {
                BrandId = car.Brand?.Id ?? 0,
                CategoryId = car.Category?.Id ?? 0,
                Model = car.Model,
                Year = car.Year,
                Price = car.Price,
                Mileage = car.Mileage,
                FuelType = car.FuelType,
                Transmission = car.Transmission,
                Color = car.Color,
                Description = car.Description,
                ImageUrl = car.MainImage,
                Status = string.IsNullOrWhiteSpace(car.Status) ? "Active" : car.Status
            }
        };
    }

    private async Task<CarLookupDto> SafeLookupsAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _cars.GetLookupsAsync(cancellationToken);
        }
        catch (CarApiException)
        {
            ViewData["ServiceUnavailable"] = true;
            return new CarLookupDto();
        }
    }
}
