using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

public abstract class AdminControllerBase : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApiAccessTokenService _tokenService;

    protected AdminControllerBase(
        UserManager<ApplicationUser> userManager,
        IApiAccessTokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    protected async Task<string?> CreateTokenAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        return _tokenService.CreateToken(User, roles);
    }

    protected UserManager<ApplicationUser> UserManager => _userManager;
}
