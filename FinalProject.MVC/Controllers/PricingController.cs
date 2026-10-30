using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

/// <summary>
/// Theme pricing plans are not part of AutoVault (contact / in-store only).
/// Keep the /Pricing route and send visitors to Contact.
/// </summary>
public class PricingController : Controller
{
    public IActionResult Index()
    {
        return RedirectToActionPermanent("Index", "Contact");
    }
}
