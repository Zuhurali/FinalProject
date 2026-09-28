using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class ShopController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Shop - AutoVault";
            return View();
        }

        public IActionResult ShopTwo()
        {
            ViewData["Title"] = "Shop Two - AutoVault";
            return View();
        }

        public IActionResult ShopThree()
        {
            ViewData["Title"] = "Shop Three - AutoVault";
            return View();
        }

        public IActionResult Details(int? id)
        {
            ViewData["Title"] = "Shop Details - AutoVault";
            ViewData["ProductId"] = id ?? 1;
            return View();
        }

        public IActionResult Cart()
        {
            ViewData["Title"] = "Cart - AutoVault";
            return View();
        }

        public IActionResult Checkout()
        {
            ViewData["Title"] = "Checkout - AutoVault";
            return View();
        }
    }
}