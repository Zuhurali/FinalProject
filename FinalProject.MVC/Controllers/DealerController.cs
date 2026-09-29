using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class DealerController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Car Dealer - AutoVault";
            return View();
        }

        public IActionResult Details(int? id)
        {
            ViewData["Title"] = "Dealer Details - AutoVault";
            ViewData["DealerId"] = id ?? 1;
            return View();
        }
    }
}