using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

public class FaqController : Controller
{
    private readonly IFaqService _faqs;

    public FaqController(IFaqService faqs)
    {
        _faqs = faqs;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "FAQ - AutoVault";
        return View(await _faqs.GetPublishedAsync(cancellationToken));
    }
}
