using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class ServicesController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Services - AutoVault";
            return View();
        }
    }
}