using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Dealers")]
public sealed class AdminDealersController : Controller
{
    private readonly IDealerService _dealers;

    public AdminDealersController(IDealerService dealers)
    {
        _dealers = dealers;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Dealers - AutoVault";
        return View(await _dealers.GetAllAsync(cancellationToken));
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add dealer - AutoVault";
        return View(new DealerEditViewModel
        {
            IsPublished = true,
            SortOrder = 0,
            ContactRole = "Car Dealer",
            FeatureList = DealerFeatureDefaults.FeatureList
        });
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Create(
        DealerEditViewModel model,
        IFormFile? cardImage,
        IFormFile? detailImage,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add dealer - AutoVault";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (ok, error, _) = await _dealers.CreateAsync(
            model,
            new DealerImageUploads { CardImage = cardImage, DetailImage = detailImage },
            cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "The dealer could not be created.");
            return View(model);
        }

        TempData["Success"] = "Dealer was created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit dealer - AutoVault";
        var dealer = await _dealers.GetByIdAsync(id, cancellationToken);
        if (dealer == null)
        {
            return NotFound();
        }

        return View(ToEditModel(dealer));
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Edit(
        int id,
        DealerEditViewModel model,
        IFormFile? cardImage,
        IFormFile? detailImage,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit dealer - AutoVault";
        model.Id = id;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (ok, error) = await _dealers.UpdateAsync(
            model,
            new DealerImageUploads { CardImage = cardImage, DetailImage = detailImage },
            cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "The dealer could not be updated.");
            return View(model);
        }

        TempData["Success"] = "Dealer was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _dealers.DeleteAsync(id, cancellationToken);
        TempData[deleted ? "Success" : "Error"] = deleted
            ? $"Dealer #{id} was deleted."
            : "Dealer not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Publish/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, bool publish, CancellationToken cancellationToken)
    {
        var updated = await _dealers.SetPublishedAsync(id, publish, cancellationToken);
        TempData[updated ? "Success" : "Error"] = updated
            ? (publish ? $"Dealer #{id} is published." : $"Dealer #{id} is unpublished.")
            : "Dealer not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Move/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int direction, CancellationToken cancellationToken)
    {
        var (ok, error) = await _dealers.MoveAsync(id, direction, cancellationToken);
        TempData[ok ? "Success" : "Error"] = ok ? "Dealer order was updated." : error ?? "Move failed.";
        return RedirectToAction(nameof(Index));
    }

    private static DealerEditViewModel ToEditModel(Dealer dealer) => new()
    {
        Id = dealer.Id,
        Name = dealer.Name,
        CardImage = dealer.CardImage,
        DetailImage = dealer.DetailImage,
        Summary = dealer.Summary,
        Phone = dealer.Phone,
        Email = dealer.Email,
        Address = dealer.Address,
        Description = dealer.Description,
        DescriptionExtra = dealer.DescriptionExtra,
        FeatureList = dealer.FeatureList,
        CarType = dealer.CarType,
        Mileage = dealer.Mileage,
        FuelType = dealer.FuelType,
        Transmission = dealer.Transmission,
        Person = dealer.Person,
        DriveType = dealer.DriveType,
        Engine = dealer.Engine,
        Color = dealer.Color,
        ContactName = dealer.ContactName,
        ContactRole = dealer.ContactRole,
        Whatsapp = dealer.Whatsapp,
        ContactPhone = dealer.ContactPhone,
        SpecTransmission = dealer.SpecTransmission,
        ChargingTime = dealer.ChargingTime,
        BatteryCapacity = dealer.BatteryCapacity,
        FastCharge = dealer.FastCharge,
        BatteryWarranty = dealer.BatteryWarranty,
        PowertrainWarranty = dealer.PowertrainWarranty,
        Displacement = dealer.Displacement,
        Torque = dealer.Torque,
        Horsepower = dealer.Horsepower,
        Cylinders = dealer.Cylinders,
        EngineLayout = dealer.EngineLayout,
        EngineConfig = dealer.EngineConfig,
        SortOrder = dealer.SortOrder,
        IsPublished = dealer.IsPublished
    };
}
