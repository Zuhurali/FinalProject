using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/FaqPage")]
public sealed class AdminFaqPageController : Controller
{
    private readonly IFaqPageContentService _page;
    public AdminFaqPageController(IFaqPageContentService page) => _page = page;

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "FAQ Page - AutoVault";
        var c = _page.GetPublic();
        return View(new FaqPageContentEditViewModel
        {
            BreadcrumbTitle = c.BreadcrumbTitle,
            Eyebrow = c.Eyebrow,
            HeadingBefore = c.HeadingBefore,
            HeadingHighlight = c.HeadingHighlight,
            BannerImagePath = c.BannerImagePath,
            SeoTitle = c.SeoTitle,
            SeoDescription = c.SeoDescription
        });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<IActionResult> Index(FaqPageContentEditViewModel model, IFormFile? banner, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "FAQ Page - AutoVault";
        if (!ModelState.IsValid) return View(model);
        var (ok, error) = await _page.SaveAsync(model, banner, cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Could not save FAQ page.");
            return View(model);
        }

        TempData["Success"] = "FAQ page content was saved.";
        return RedirectToAction(nameof(Index));
    }
}

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/TeamPage")]
public sealed class AdminTeamPageController : Controller
{
    private readonly ITeamPageContentService _page;
    public AdminTeamPageController(ITeamPageContentService page) => _page = page;

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Team Page - AutoVault";
        var c = _page.GetPublic();
        return View(new TeamPageContentEditViewModel
        {
            BreadcrumbTitle = c.BreadcrumbTitle,
            Eyebrow = c.Eyebrow,
            HeadingBefore = c.HeadingBefore,
            HeadingHighlight = c.HeadingHighlight,
            SeoTitle = c.SeoTitle,
            SeoDescription = c.SeoDescription
        });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(TeamPageContentEditViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Team Page - AutoVault";
        if (!ModelState.IsValid) return View(model);
        await _page.SaveAsync(model, cancellationToken);
        TempData["Success"] = "Team page content was saved.";
        return RedirectToAction(nameof(Index));
    }
}

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/DealerPage")]
public sealed class AdminDealerPageController : Controller
{
    private readonly IDealerPageContentService _page;
    public AdminDealerPageController(IDealerPageContentService page) => _page = page;

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Dealer Page - AutoVault";
        var c = _page.GetPublic();
        return View(new DealerPageContentEditViewModel
        {
            BreadcrumbTitle = c.BreadcrumbTitle,
            PageTitle = c.PageTitle,
            SeoTitle = c.SeoTitle,
            SeoDescription = c.SeoDescription
        });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(DealerPageContentEditViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Dealer Page - AutoVault";
        if (!ModelState.IsValid) return View(model);
        await _page.SaveAsync(model, cancellationToken);
        TempData["Success"] = "Dealer page content was saved.";
        return RedirectToAction(nameof(Index));
    }
}

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/CalculatorPage")]
public sealed class AdminCalculatorPageController : Controller
{
    private readonly ICalculatorPageContentService _page;
    public AdminCalculatorPageController(ICalculatorPageContentService page) => _page = page;

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Calculator Page - AutoVault";
        var c = _page.GetPublic();
        return View(new CalculatorPageContentEditViewModel
        {
            BreadcrumbTitle = c.BreadcrumbTitle,
            PageTitle = c.PageTitle,
            IntroText = c.IntroText,
            SeoTitle = c.SeoTitle,
            SeoDescription = c.SeoDescription
        });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CalculatorPageContentEditViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Calculator Page - AutoVault";
        if (!ModelState.IsValid) return View(model);
        await _page.SaveAsync(model, cancellationToken);
        TempData["Success"] = "Calculator page content was saved.";
        return RedirectToAction(nameof(Index));
    }
}
