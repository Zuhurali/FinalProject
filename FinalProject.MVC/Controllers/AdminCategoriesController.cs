using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Security;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FinalProject.MVC.Models;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Categories")]
public sealed class AdminCategoriesController : AdminControllerBase
{
    private readonly IAdminApiService _adminApi;
    private readonly ICarApiService _cars;

    public AdminCategoriesController(
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
        ViewData["Title"] = "Categories - AutoVault";
        try
        {
            return View(await _cars.GetLookupsAsync(cancellationToken));
        }
        catch (CarApiException)
        {
            ViewData["ServiceUnavailable"] = true;
            return View(new CarLookupDto());
        }
    }

    [Authorize(Roles = AppRoles.SuperAdmin)]
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Create(string name, CancellationToken cancellationToken) =>
        Change(() => Act(token => _adminApi.CreateCategoryAsync(name, token, cancellationToken)), "The category was created.");

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, string name, CancellationToken cancellationToken) =>
        Change(() => Act(token => _adminApi.UpdateCategoryAsync(id, name, token, cancellationToken)), "The category was updated.");

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        Change(() => Act(token => _adminApi.DeleteCategoryAsync(id, token, cancellationToken)), "The category was deleted.");

    private async Task<IActionResult> Change(Func<Task> action, string success)
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

    private async Task Act(Func<string, Task> action)
    {
        var token = await CreateTokenAsync();
        if (token == null)
        {
            throw new CarApiException("Sign in is required.");
        }

        await action(token);
    }
}
