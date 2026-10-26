using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Settings")]
public sealed class AdminSettingsController : Controller
{
    private readonly ISiteSettingsService _siteSettings;

    public AdminSettingsController(ISiteSettingsService siteSettings)
    {
        _siteSettings = siteSettings;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Site Settings - AutoVault";
        var current = _siteSettings.GetPublic();
        return View(ToEditModel(current));
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> Index(
        SiteSettingsEditViewModel model,
        IFormFile? logoFile,
        IFormFile? footerLogoFile,
        IFormFile? faviconFile,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Site Settings - AutoVault";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (ok, error) = await _siteSettings.SaveAsync(
            model,
            logoFile,
            footerLogoFile,
            faviconFile,
            cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "The settings could not be saved.");
            return View(model);
        }

        TempData["Success"] = "Site settings were saved.";
        return RedirectToAction(nameof(Index));
    }

    private static SiteSettingsEditViewModel ToEditModel(SitePublicSettings current) => new()
    {
        SiteName = current.SiteName,
        DisplayPhone = current.Contact.DisplayPhone,
        E164Phone = current.Contact.E164Phone,
        WhatsAppDigits = current.Contact.WhatsAppDigits,
        Email = current.Contact.Email,
        AddressLine = current.Contact.AddressLine,
        MapsQuery = current.Contact.MapsQuery,
        LogoPath = current.LogoPath,
        FooterLogoPath = current.FooterLogoPath,
        FaviconPath = current.FaviconPath,
        FooterText = current.FooterText,
        CopyrightText = current.CopyrightText,
        FacebookUrl = current.FacebookUrl,
        InstagramUrl = current.InstagramUrl,
        YoutubeUrl = current.YoutubeUrl,
        TwitterUrl = current.TwitterUrl,
        DefaultSeoTitle = current.DefaultSeoTitle,
        DefaultMetaDescription = current.DefaultMetaDescription
    };
}
