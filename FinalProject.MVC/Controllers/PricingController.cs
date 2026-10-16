using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class PricingController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToActionPermanent("Index", "Contact");
        }
    }
}
