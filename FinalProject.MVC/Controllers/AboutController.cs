using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "About Us - AutoVault";
            return View();
        }
    }
}