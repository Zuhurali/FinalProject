using FinalProject.MVC.Models;
using FinalProject.MVC.Security;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Products")]
public sealed class AdminProductsController : AdminControllerBase
{
    public AdminProductsController(
        UserManager<ApplicationUser> userManager,
        IApiAccessTokenService tokenService)
        : base(userManager, tokenService)
    {
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return RedirectToAction("Index", "AdminParts");
    }

    [Authorize(Roles = AppRoles.SuperAdmin)]
    [HttpGet("Create")]
    public IActionResult Create()
    {
        return RedirectToAction("Create", "AdminParts");
    }

    [Authorize(Roles = AppRoles.SuperAdmin)]
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public IActionResult CreatePost()
    {
        return RedirectToAction("Create", "AdminParts");
    }

    [HttpGet("Edit/{id:int}")]
    public IActionResult Edit(int id)
    {
        return RedirectToAction("Edit", "AdminParts", new { id });
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public IActionResult EditPost(int id)
    {
        return RedirectToAction("Edit", "AdminParts", new { id });
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        return RedirectToAction("Index", "AdminParts");
    }
}
