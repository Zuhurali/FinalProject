using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

public class FaqController : Controller
{
    private readonly IFaqService _faqs;
    private readonly IFaqPageContentService _faqPage;

    public FaqController(IFaqService faqs, IFaqPageContentService faqPage)
    {
        _faqs = faqs;
        _faqPage = faqPage;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var page = _faqPage.GetPublic();
        ViewData["Title"] = page.SeoTitle;
        ViewData["MetaDescription"] = page.SeoDescription;
        ViewData["FaqPage"] = page;
        return View(await _faqs.GetPublishedAsync(cancellationToken));
    }
}
