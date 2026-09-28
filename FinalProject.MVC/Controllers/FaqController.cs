using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class FaqController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "FAQ - AutoVault";
            return View();
        }
    }
}