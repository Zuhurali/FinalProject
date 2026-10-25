using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Security;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Parts")]
public sealed class AdminPartsController : AdminControllerBase
{
    private readonly IAdminApiService _adminApi;
    private readonly IWebHostEnvironment _environment;

    public AdminPartsController(
        UserManager<ApplicationUser> userManager,
        IApiAccessTokenService tokenService,
        IAdminApiService adminApi,
        IWebHostEnvironment environment)
        : base(userManager, tokenService)
    {
        _adminApi = adminApi;
        _environment = environment;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string? category, int page = 1, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Parts - AutoVault";
        ViewData["Search"] = search;
        ViewData["Category"] = category;
        try
        {
            var token = await RequireTokenAsync();
            return View(await _adminApi.GetPartsAsync(search, category, page, token, cancellationToken));
        }
        catch (CarApiException exception)
        {
            TempData["Error"] = exception.Message;
            return View(new PartAdminListDto());
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Part details - AutoVault";
        try
        {
            var token = await RequireTokenAsync();
            return View(await _adminApi.GetPartAsync(id, token, cancellationToken));
        }
        catch (CarApiException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Roles = AppRoles.SuperAdmin)]
    [HttpGet("Create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add part - AutoVault";
        return View("Edit", new SavePartDto { IsActive = true });
    }

    [Authorize(Roles = AppRoles.SuperAdmin)]
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "")] SavePartDto part, IFormFile? image, CancellationToken cancellationToken)
    {
        var imageError = await ApplyImageAsync(part, image, required: string.IsNullOrWhiteSpace(part.ImagePath));
        if (imageError != null)
        {
            ModelState.AddModelError(nameof(part.ImagePath), imageError);
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Add part - AutoVault";
            RememberFormError();
            return View("Edit", part);
        }

        try
        {
            var token = await RequireTokenAsync();
            part.IsActive = true;
            await _adminApi.CreatePartAsync(part, token, cancellationToken);
            TempData["Success"] = "The part was created.";
            return RedirectToAction(nameof(Index));
        }
        catch (CarApiException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            RememberFormError();
            return View("Edit", part);
        }
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit part - AutoVault";
        try
        {
            var token = await RequireTokenAsync();
            var existing = await _adminApi.GetPartAsync(id, token, cancellationToken);
            return View(ToForm(existing));
        }
        catch (CarApiException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(Prefix = "")] SavePartDto part, IFormFile? image, CancellationToken cancellationToken)
    {
        part.Id = id;
        var imageError = await ApplyImageAsync(part, image, required: false);
        if (imageError != null)
        {
            ModelState.AddModelError(nameof(part.ImagePath), imageError);
        }

        if (!ModelState.IsValid)
        {
            RememberFormError();
            return View(part);
        }

        try
        {
            var token = await RequireTokenAsync();
            await _adminApi.UpdatePartAsync(id, part, token, cancellationToken);
            TempData["Success"] = "Part updated successfully.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (CarApiException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            RememberFormError();
            return View(part);
        }
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var token = await RequireTokenAsync();
            var result = await _adminApi.DeletePartAsync(id, token, cancellationToken);
            TempData["Success"] = result.Message;
        }
        catch (CarApiException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private void RememberFormError()
    {
        var messages = ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct()
            .ToList();
        TempData["Error"] = messages.Count == 0
            ? "The part could not be saved."
            : string.Join(" ", messages);
    }

    private async Task<string> RequireTokenAsync()
    {
        var token = await CreateTokenAsync();
        if (token == null)
        {
            throw new CarApiException("Sign in is required.");
        }

        return token;
    }

    private async Task<string?> ApplyImageAsync(SavePartDto part, IFormFile? image, bool required)
    {
        if (image == null || image.Length == 0)
        {
            return required ? "Add a product image." : null;
        }

        var error = PartImageRules.Validate(image.FileName, image.Length);
        if (error != null)
        {
            return error;
        }

        var folder = Path.Combine(_environment.WebRootPath, "assets", "images", "portfolio", "parts", "uploads");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(image.FileName).ToLowerInvariant()}";
        var physical = Path.Combine(folder, fileName);
        await using var stream = System.IO.File.Create(physical);
        await image.CopyToAsync(stream);
        part.ImagePath = $"~/assets/images/portfolio/parts/uploads/{fileName}";
        return null;
    }

    private static SavePartDto ToForm(PartAdminItemDto part) => new()
    {
        Id = part.Id,
        Name = part.Name,
        Description = part.Description,
        Price = part.Price,
        Category = part.Category,
        Brand = part.Brand,
        Compatibility = part.Compatibility,
        Quantity = part.Quantity,
        ImagePath = part.ImagePath,
        IsActive = part.IsActive
    };
}
