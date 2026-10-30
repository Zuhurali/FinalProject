using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

/// <summary>
/// Theme repair/service-center marketing is not the live AutoVault offer.
/// Keep the /Services route and send visitors to Contact.
/// </summary>
public class ServicesController : Controller
{
    public IActionResult Index()
    {
        return RedirectToActionPermanent("Index", "Contact");
    }
}
