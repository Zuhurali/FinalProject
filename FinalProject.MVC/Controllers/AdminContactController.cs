using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Contact")]
public sealed class AdminContactController : Controller
{
    private readonly IContactPageContentService _contactPage;

    public AdminContactController(IContactPageContentService contactPage)
    {
        _contactPage = contactPage;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Contact Page - AutoVault";
        return View(ToEditModel(_contactPage.GetPublic()));
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ContactPageContentEditViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Contact Page - AutoVault";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (ok, error) = await _contactPage.SaveAsync(model, cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "The contact page could not be saved.");
            return View(model);
        }

        TempData["Success"] = "Contact page content was saved.";
        return RedirectToAction(nameof(Index));
    }

    private static ContactPageContentEditViewModel ToEditModel(ContactPagePublicContent current) => new()
    {
        PageTitle = current.PageTitle,
        SeoTitle = current.SeoTitle,
        SeoDescription = current.SeoDescription,
        FormTitle = current.FormTitle,
        FormIntro = current.FormIntro,
        LocationCardTitle = current.LocationCardTitle,
        PhoneCardTitle = current.PhoneCardTitle,
        EmailCardTitle = current.EmailCardTitle,
        MapEmbedUrl = current.MapEmbedUrl,
        MapTitle = current.MapTitle
    };
}
