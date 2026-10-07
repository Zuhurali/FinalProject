using FinalProject.MVC.Models;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Security;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Brands")]
public sealed class AdminBrandsController : AdminControllerBase
{
    private readonly IAdminApiService _adminApi;
    private readonly ICarApiService _cars;

    public AdminBrandsController(
        UserManager<ApplicationUser> userManager,
        IApiAccessTokenService tokenService,
        IAdminApiService adminApi,
        ICarApiService cars)
        : base(userManager, tokenService)
    {
        _adminApi = adminApi;
        _cars = cars;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Brands - AutoVault";
        return View(await LoadAsync(cancellationToken));
    }

    [Authorize(Roles = AppRoles.SuperAdmin)]
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Create(string name, CancellationToken cancellationToken) =>
        ChangeAsync(() => TokenAction(token => _adminApi.CreateBrandAsync(name, token, cancellationToken)), "The brand was created.");

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, string name, CancellationToken cancellationToken) =>
        ChangeAsync(() => TokenAction(token => _adminApi.UpdateBrandAsync(id, name, token, cancellationToken)), "The brand was updated.");

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        ChangeAsync(() => TokenAction(token => _adminApi.DeleteBrandAsync(id, token, cancellationToken)), "The brand was deleted.");

    private async Task<CarLookupDto> LoadAsync(CancellationToken cancellationToken)
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

    private async Task<IActionResult> ChangeAsync(Func<Task> action, string success)
    {
        try
        {
            await action();
            TempData["Success"] = success;
        }
        catch (CarApiException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task TokenAction(Func<string, Task> action)
    {
        var token = await CreateTokenAsync();
        if (token == null)
        {
            throw new CarApiException("Sign in is required.");
        }

        await action(token);
    }
}
