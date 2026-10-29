using FinalProject.MVC.Hubs;
using FinalProject.MVC.Models;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Security;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Cars")]
public sealed class AdminCarsController : AdminControllerBase
{
    private const int DefaultPageSize = 20;

    private readonly ICarApiService _cars;
    private readonly ICarImageUploadService _images;
    private readonly IHubContext<ConversationHub> _conversations;
    private readonly ILogger<AdminCarsController> _logger;

    public AdminCarsController(
        UserManager<ApplicationUser> userManager,
        IApiAccessTokenService tokenService,
        ICarApiService carApi,
        ICarImageUploadService images,
        IHubContext<ConversationHub> conversations,
        ILogger<AdminCarsController> logger)
        : base(userManager, tokenService)
    {
        _cars = carApi;
        _images = images;
        _conversations = conversations;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search,
        int? brandId,
        string? status,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Admin Cars - AutoVault";
        if (page < 1)
        {
            page = 1;
        }

        var model = new AdminCarsIndexViewModel
        {
            Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            BrandId = brandId is > 0 ? brandId : null,
            Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
            Page = page,
            PageSize = DefaultPageSize
        };

        try
        {
            var lookups = await SafeLookupsAsync(cancellationToken);
            model.Brands = lookups.Brands
                .OrderBy(brand => brand.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var pageResult = await _cars.GetCarsAsync(
                new CarApiQuery
                {
                    Search = model.Search,
                    BrandId = model.BrandId,
                    Status = model.Status,
                    Page = model.Page,
                    PageSize = model.PageSize
                },
                cancellationToken);

            model.Items = pageResult.Items;
            model.TotalCount = pageResult.TotalCount;
            model.TotalPages = pageResult.TotalPages;
            model.Page = pageResult.CurrentPage > 0 ? pageResult.CurrentPage : page;
            model.PageSize = pageResult.PageSize > 0 ? pageResult.PageSize : DefaultPageSize;
        }
        catch (CarApiException)
        {
            ViewData["ServiceUnavailable"] = true;
        }

        return View(model);
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Car Details - AutoVault";
        try
        {
            var token = await CreateTokenAsync();
            var car = token == null
                ? null
                : await _cars.GetCarByIdAsync(id, token, cancellationToken);
            return car == null ? NotFound() : View(car);
        }
        catch (CarApiException)
        {
            ViewData["ServiceUnavailable"] = true;
            return View((CarApiDto?)null);
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
    [RequestSizeLimit(64 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 64 * 1024 * 1024)]
    public async Task<IActionResult> Create(
        CarEditorViewModel model,
        List<IFormFile>? vehiclePhotos,
        CancellationToken cancellationToken)
    {
        model.Car.GallerySpecified = true;
        var uploaded = new List<string>();
        if (!await PrepareImagesAsync(model, vehiclePhotos, uploaded, requireImage: true, cancellationToken))
        {
            return View(model);
        }

        var token = await CreateTokenAsync();
        if (token == null)
        {
            _images.DeleteUploads(uploaded);
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
            _images.DeleteUploads(uploaded);
            if (uploaded.Contains(model.Car.ImageUrl))
            {
                model.Car.ImageUrl = string.Empty;
            }

            model.Car.AdditionalImageUrls.Clear();
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
    [RequestSizeLimit(64 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 64 * 1024 * 1024)]
    public async Task<IActionResult> Edit(
        int id,
        CarEditorViewModel model,
        List<IFormFile>? vehiclePhotos,
        CancellationToken cancellationToken)
    {
        model.Id = id;
        model.Car.GallerySpecified = true;
        var previousImage = model.Car.ImageUrl;
        var uploaded = new List<string>();
        if (!await PrepareImagesAsync(model, vehiclePhotos, uploaded, requireImage: true, cancellationToken))
        {
            return View(model);
        }

        var token = await CreateTokenAsync();
        if (token == null)
        {
            _images.DeleteUploads(uploaded);
            return Challenge();
        }

        try
        {
            var update = await _cars.UpdateCarAsync(id, model.Car, token, cancellationToken);
            _images.DeleteUploads(update.RemovedImageUrls);
            foreach (var notification in update.Notifications)
            {
                try
                {
                    await _conversations.Clients
                        .Group(ConversationHub.InboxGroupName(notification.UserId))
                        .SendAsync(
                            ConversationHub.NotificationReceivedEvent,
                            notification,
                            cancellationToken);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Notification {NotificationId} was saved but not broadcast.",
                        notification.Id);
                }
            }

            TempData["Success"] = "The car was updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (CarApiException exception)
        {
            _images.DeleteUploads(uploaded);
            if (uploaded.Count > 0)
            {
                model.Car.ImageUrl = previousImage;
            }

            model.Car.AdditionalImageUrls.Clear();
            ModelState.AddModelError(string.Empty, exception.Message);
            model.Id = id;
            await ReloadEditorAsync(model, cancellationToken);
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

    private async Task<bool> PrepareImagesAsync(
        CarEditorViewModel model,
        IEnumerable<IFormFile>? vehiclePhotos,
        List<string> uploaded,
        bool requireImage,
        CancellationToken cancellationToken)
    {
        var previousImage = model.Car.ImageUrl;
        var photos = (vehiclePhotos ?? Array.Empty<IFormFile>())
            .Where(file => file is { Length: > 0 })
            .ToList();
        var error = await _images.ApplyVehiclePhotosAsync(model.Car, photos, requireImage, cancellationToken);
        if (error != null)
        {
            ModelState.AddModelError("Car.ImageUrl", error);
        }
        else if (!string.IsNullOrWhiteSpace(model.Car.ImageUrl))
        {
            ModelState.Remove("Car.ImageUrl");
        }

        if (ModelState.IsValid)
        {
            if (photos.Count > 0)
            {
                uploaded.Add(model.Car.ImageUrl);
                uploaded.AddRange(model.Car.AdditionalImageUrls);
            }

            return true;
        }

        _images.DeleteUploads(uploaded);
        uploaded.Clear();
        if (photos.Count > 0)
        {
            model.Car.ImageUrl = previousImage;
        }

        model.Car.AdditionalImageUrls.Clear();
        await ReloadEditorAsync(model, cancellationToken);
        return false;
    }

    private async Task<CarEditorViewModel?> EditorAsync(int? id, CancellationToken cancellationToken)
    {
        var lookups = await SafeLookupsAsync(cancellationToken);
        if (id == null)
        {
            return new CarEditorViewModel { Lookups = lookups };
        }

        var token = await CreateTokenAsync();
        var car = token == null
            ? null
            : await _cars.GetCarByIdAsync(id.Value, token, cancellationToken);
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
                Status = string.IsNullOrWhiteSpace(car.Status) ? "Active" : car.Status,
                IsFeatured = car.IsFeatured,
                Seats = car.Seats,
                DriveType = car.DriveType,
                Engine = car.Engine,
                MileageUnit = car.MileageUnit,
                Horsepower = car.Horsepower,
                Doors = car.Doors,
                Cylinders = car.Cylinders,
                Steering = car.Steering,
                Vin = car.Vin,
                AccidentHistory = car.AccidentHistory,
                RepaintedParts = car.RepaintedParts,
                ReplacedParts = car.ReplacedParts,
                CustomsStatus = car.CustomsStatus,
                MarketOrigin = car.MarketOrigin,
                Condition = car.IsNew ? "New" : "Used",
                Features = car.Features ?? new List<string>()
            },
            Gallery = car.Images
                .OrderBy(image => image.DisplayOrder)
                .ThenBy(image => image.Id)
                .ToList()
        };
    }

    private async Task ReloadEditorAsync(CarEditorViewModel model, CancellationToken cancellationToken)
    {
        model.Lookups = await SafeLookupsAsync(cancellationToken);
        if (model.Id is not int id)
        {
            return;
        }

        try
        {
            var token = await CreateTokenAsync();
            var car = token == null
                ? null
                : await _cars.GetCarByIdAsync(id, token, cancellationToken);
            model.Gallery = car?.Images
                .OrderBy(image => image.DisplayOrder)
                .ThenBy(image => image.Id)
                .ToList() ?? new List<CarImageApiDto>();
        }
        catch (CarApiException)
        {
            ViewData["ServiceUnavailable"] = true;
        }
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
