using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/About")]
public sealed class AdminAboutController : Controller
{
    private readonly IAboutPageContentService _aboutPage;

    public AdminAboutController(IAboutPageContentService aboutPage)
    {
        _aboutPage = aboutPage;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "About Page - AutoVault";
        return View(ToEditModel(_aboutPage.GetPublic()));
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
    public async Task<IActionResult> Index(
        AboutPageContentEditViewModel model,
        IFormFile? imageLeft,
        IFormFile? imageRight,
        IFormFile? showcase1Image,
        IFormFile? showcase2Image,
        IFormFile? showcase3Image,
        IFormFile? showcase4Image,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "About Page - AutoVault";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var uploads = new AboutPageImageUploads
        {
            ImageLeft = imageLeft,
            ImageRight = imageRight,
            Showcase1Image = showcase1Image,
            Showcase2Image = showcase2Image,
            Showcase3Image = showcase3Image,
            Showcase4Image = showcase4Image
        };
        var (ok, error) = await _aboutPage.SaveAsync(model, uploads, cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "The about page could not be saved.");
            return View(model);
        }

        TempData["Success"] = "About page content was saved.";
        return RedirectToAction(nameof(Index));
    }

    private static AboutPageContentEditViewModel ToEditModel(AboutPagePublicContent current) => new()
    {
        PageTitle = current.PageTitle,
        SeoTitle = current.SeoTitle,
        SeoDescription = current.SeoDescription,
        BreadcrumbTitle = current.BreadcrumbTitle,
        Eyebrow = current.Eyebrow,
        HeadingBefore = current.HeadingBefore,
        HeadingHighlight = current.HeadingHighlight,
        Body = current.Body,
        ImageLeftPath = current.ImageLeftPath,
        ImageRightPath = current.ImageRightPath,
        CallCtaLabel = current.CallCtaLabel,
        ShowcaseEyebrow = current.ShowcaseEyebrow,
        ShowcaseTitle = current.ShowcaseTitle,
        ShowcaseDescription = current.ShowcaseDescription,
        Showcase1Name = current.Showcase1Name,
        Showcase1Descriptor = current.Showcase1Descriptor,
        Showcase1ImagePath = current.Showcase1ImagePath,
        Showcase2Name = current.Showcase2Name,
        Showcase2Descriptor = current.Showcase2Descriptor,
        Showcase2ImagePath = current.Showcase2ImagePath,
        Showcase3Name = current.Showcase3Name,
        Showcase3Descriptor = current.Showcase3Descriptor,
        Showcase3ImagePath = current.Showcase3ImagePath,
        Showcase4Name = current.Showcase4Name,
        Showcase4Descriptor = current.Showcase4Descriptor,
        Showcase4ImagePath = current.Showcase4ImagePath
    };
}
