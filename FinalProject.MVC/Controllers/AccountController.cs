using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Account - AutoVault";
            return View();
        }

        public IActionResult SignIn()
        {
            ViewData["Title"] = "Sign In - AutoVault";
            return View();
        }

        public IActionResult AddCar()
        {
            ViewData["Title"] = "Add Car - AutoVault";
            return View();
        }
    }
}