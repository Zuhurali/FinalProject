using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Models;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Security;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.SuperAdmin)]
[Route("Admin/Statistics")]
public sealed class AdminStatisticsController : AdminControllerBase
{
    private readonly ICarApiService _cars;
    private readonly ILogger<AdminStatisticsController> _logger;

    public AdminStatisticsController(
        UserManager<ApplicationUser> userManager,
        IApiAccessTokenService tokenService,
        ICarApiService cars,
        ILogger<AdminStatisticsController> logger)
        : base(userManager, tokenService)
    {
        _cars = cars;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Car Statistics - AutoVault";
        var token = await CreateTokenAsync();
        if (token == null)
        {
            return Challenge();
        }

        try
        {
            return View(await _cars.GetDiscoveryStatisticsAsync(token, cancellationToken));
        }
        catch (CarApiException exception)
        {
            _logger.LogError(exception, "Car statistics could not be loaded.");
            ViewData["ServiceUnavailable"] = true;
            return View(new CarDiscoveryStatisticsDto());
        }
    }
}
