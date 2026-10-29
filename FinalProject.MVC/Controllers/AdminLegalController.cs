using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Legal")]
public sealed class AdminLegalController : Controller
{
    private readonly ILegalPageContentService _legalPage;

    public AdminLegalController(ILegalPageContentService legalPage)
    {
        _legalPage = legalPage;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Legal Pages - AutoVault";
        return View(ToEditModel(_legalPage.GetPublic()));
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LegalPageContentEditViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Legal Pages - AutoVault";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (ok, error) = await _legalPage.SaveAsync(model, cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Legal content could not be saved.");
            return View(model);
        }

        TempData["Success"] = "Legal page content was saved.";
        return RedirectToAction(nameof(Index));
    }

    private static LegalPageContentEditViewModel ToEditModel(LegalPagePublicContent current) => new()
    {
        PrivacyTitle = current.PrivacyTitle,
        PrivacyBody = current.PrivacyBody,
        PrivacySeoTitle = current.PrivacySeoTitle,
        PrivacySeoDescription = current.PrivacySeoDescription,
        TermsTitle = current.TermsTitle,
        TermsBody = current.TermsBody,
        TermsSeoTitle = current.TermsSeoTitle,
        TermsSeoDescription = current.TermsSeoDescription
    };
}
