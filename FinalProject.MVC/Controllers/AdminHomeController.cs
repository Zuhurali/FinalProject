using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Home")]
public sealed class AdminHomeController : Controller
{
    private readonly IHomePageContentService _homePage;

    public AdminHomeController(IHomePageContentService homePage)
    {
        _homePage = homePage;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Home Page - AutoVault";
        return View(ToEditModel(_homePage.GetPublic()));
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
    public async Task<IActionResult> Index(
        HomePageContentEditViewModel model,
        IFormFile? heroImage,
        IFormFile? aboutImageMain,
        IFormFile? aboutImageFloat1,
        IFormFile? aboutImageFloat2,
        IFormFile? whyChooseBackground,
        IFormFile? feature1Icon,
        IFormFile? feature2Icon,
        IFormFile? feature3Icon,
        IFormFile? feature4Icon,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Home Page - AutoVault";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var uploads = new HomePageImageUploads
        {
            HeroImage = heroImage,
            AboutImageMain = aboutImageMain,
            AboutImageFloat1 = aboutImageFloat1,
            AboutImageFloat2 = aboutImageFloat2,
            WhyChooseBackground = whyChooseBackground,
            Feature1Icon = feature1Icon,
            Feature2Icon = feature2Icon,
            Feature3Icon = feature3Icon,
            Feature4Icon = feature4Icon
        };
        var (ok, error) = await _homePage.SaveAsync(model, uploads, cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "The homepage content could not be saved.");
            return View(model);
        }

        TempData["Success"] = "Home page content was saved.";
        return RedirectToAction(nameof(Index));
    }

    private static HomePageContentEditViewModel ToEditModel(HomePagePublicContent current) => new()
    {
        HeroTitle = current.HeroTitle,
        HeroHighlight = current.HeroHighlight,
        HeroImagePath = current.HeroImagePath,
        HeroImageAlt = current.HeroImageAlt,
        CategoriesEyebrow = current.CategoriesEyebrow,
        CategoriesHeadingBefore = current.CategoriesHeadingBefore,
        CategoriesHeadingHighlight = current.CategoriesHeadingHighlight,
        CategoriesHeadingAfter = current.CategoriesHeadingAfter,
        PortfolioEyebrow = current.PortfolioEyebrow,
        PortfolioHeadingBefore = current.PortfolioHeadingBefore,
        PortfolioHeadingHighlight = current.PortfolioHeadingHighlight,
        CampaignsEyebrow = current.CampaignsEyebrow,
        CampaignsHeadingBefore = current.CampaignsHeadingBefore,
        CampaignsHeadingHighlight = current.CampaignsHeadingHighlight,
        AboutEyebrow = current.AboutEyebrow,
        AboutHeadingBefore = current.AboutHeadingBefore,
        AboutHeadingHighlight = current.AboutHeadingHighlight,
        AboutBody = current.AboutBody,
        AboutCtaLabel = current.AboutCtaLabel,
        AboutImageMainPath = current.AboutImageMainPath,
        AboutImageFloat1Path = current.AboutImageFloat1Path,
        AboutImageFloat2Path = current.AboutImageFloat2Path,
        BrandsEyebrow = current.BrandsEyebrow,
        BrandsHeadingBefore = current.BrandsHeadingBefore,
        BrandsHeadingHighlight = current.BrandsHeadingHighlight,
        WhyChooseEyebrow = current.WhyChooseEyebrow,
        WhyChooseHeadingBefore = current.WhyChooseHeadingBefore,
        WhyChooseHeadingHighlight = current.WhyChooseHeadingHighlight,
        WhyChooseBackgroundPath = current.WhyChooseBackgroundPath,
        Feature1Title = current.Feature1Title,
        Feature1Description = current.Feature1Description,
        Feature1IconPath = current.Feature1IconPath,
        Feature2Title = current.Feature2Title,
        Feature2Description = current.Feature2Description,
        Feature2IconPath = current.Feature2IconPath,
        Feature3Title = current.Feature3Title,
        Feature3Description = current.Feature3Description,
        Feature3IconPath = current.Feature3IconPath,
        Feature4Title = current.Feature4Title,
        Feature4Description = current.Feature4Description,
        Feature4IconPath = current.Feature4IconPath,
        FeaturedEyebrow = current.FeaturedEyebrow,
        FeaturedHeadingBefore = current.FeaturedHeadingBefore,
        FeaturedHeadingHighlight = current.FeaturedHeadingHighlight
    };
}
