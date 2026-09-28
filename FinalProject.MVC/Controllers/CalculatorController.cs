using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class CalculatorController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Calculator - AutoVault";
            return View();
        }
    }
}