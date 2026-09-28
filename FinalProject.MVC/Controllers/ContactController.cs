using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Contact - AutoVault";
            return View();
        }
    }
}