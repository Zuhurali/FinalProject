using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/ShopMarketing")]
public sealed class AdminShopMarketingController : Controller
{
    private readonly IShopMarketingContentService _shopMarketing;

    public AdminShopMarketingController(IShopMarketingContentService shopMarketing)
    {
        _shopMarketing = shopMarketing;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Shop Marketing - AutoVault";
        return View(ToEditModel(_shopMarketing.GetPublic()));
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
    public async Task<IActionResult> Index(
        ShopMarketingContentEditViewModel model,
        IFormFile? heroImage,
        IFormFile? usedCarsPromoImage,
        IFormFile? visitTip1Image,
        IFormFile? visitTip2Image,
        IFormFile? why1Icon,
        IFormFile? why2Icon,
        IFormFile? why3Icon,
        IFormFile? why4Icon,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Shop Marketing - AutoVault";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var uploads = new ShopMarketingImageUploads
        {
            HeroImage = heroImage,
            UsedCarsPromoImage = usedCarsPromoImage,
            VisitTip1Image = visitTip1Image,
            VisitTip2Image = visitTip2Image,
            Why1Icon = why1Icon,
            Why2Icon = why2Icon,
            Why3Icon = why3Icon,
            Why4Icon = why4Icon
        };
        var (ok, error) = await _shopMarketing.SaveAsync(model, uploads, cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Shop marketing content could not be saved.");
            return View(model);
        }

        TempData["Success"] = "Shop marketing content was saved.";
        return RedirectToAction(nameof(Index));
    }

    private static ShopMarketingContentEditViewModel ToEditModel(ShopMarketingPublicContent current) => new()
    {
        HeroEyebrow = current.HeroEyebrow,
        HeroTitle = current.HeroTitle,
        HeroText = current.HeroText,
        HeroImagePath = current.HeroImagePath,
        HeroImageAlt = current.HeroImageAlt,
        SeoTitle = current.SeoTitle,
        SeoDescription = current.SeoDescription,
        CategoriesEyebrow = current.CategoriesEyebrow,
        CategoriesHeadingBefore = current.CategoriesHeadingBefore,
        CategoriesHeadingHighlight = current.CategoriesHeadingHighlight,
        CatalogEyebrow = current.CatalogEyebrow,
        CatalogHeadingBefore = current.CatalogHeadingBefore,
        CatalogHeadingHighlight = current.CatalogHeadingHighlight,
        CatalogIntro = current.CatalogIntro,
        WhyChooseEyebrow = current.WhyChooseEyebrow,
        WhyChooseHeadingBefore = current.WhyChooseHeadingBefore,
        WhyChooseHeadingHighlight = current.WhyChooseHeadingHighlight,
        Why1Title = current.Why1Title,
        Why1Description = current.Why1Description,
        Why2Title = current.Why2Title,
        Why2Description = current.Why2Description,
        Why3Title = current.Why3Title,
        Why3Description = current.Why3Description,
        Why4Title = current.Why4Title,
        Why4Description = current.Why4Description,
        Why1IconPath = current.Why1IconPath,
        Why2IconPath = current.Why2IconPath,
        Why3IconPath = current.Why3IconPath,
        Why4IconPath = current.Why4IconPath,
        VisitEyebrow = current.VisitEyebrow,
        VisitHeadingBefore = current.VisitHeadingBefore,
        VisitHeadingHighlight = current.VisitHeadingHighlight,
        VisitTip1Title = current.VisitTip1Title,
        VisitTip2Title = current.VisitTip2Title,
        FaqSectionEyebrow = current.FaqSectionEyebrow,
        FaqSectionHeadingBefore = current.FaqSectionHeadingBefore,
        FaqSectionHeadingHighlight = current.FaqSectionHeadingHighlight,
        ContactFormTitle = current.ContactFormTitle,
        BrandStripEyebrow = current.BrandStripEyebrow,
        BrandStripHeadingBefore = current.BrandStripHeadingBefore,
        BrandStripHeadingHighlight = current.BrandStripHeadingHighlight,
        UsedCarsPromoTag = current.UsedCarsPromoTag,
        UsedCarsPromoTitle = current.UsedCarsPromoTitle,
        UsedCarsPromoImagePath = current.UsedCarsPromoImagePath,
        VisitTip1Meta = current.VisitTip1Meta,
        VisitTip2Meta = current.VisitTip2Meta,
        VisitTip1ImagePath = current.VisitTip1ImagePath,
        VisitTip2ImagePath = current.VisitTip2ImagePath,
        HeroCtaBrowseParts = current.HeroCtaBrowseParts,
        HeroCtaBrowseCars = current.HeroCtaBrowseCars,
        HeroCtaVisit = current.HeroCtaVisit
    };
}
