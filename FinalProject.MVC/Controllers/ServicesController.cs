using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class ServicesController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToActionPermanent("Index", "Contact");
        }
    }
}
