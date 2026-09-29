using FinalProject.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FinalProject.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult DealerTwo()
        {
            ViewData["Title"] = "Dealer Two - AutoVault";
            return View();
        }

        public IActionResult DealerThree()
        {
            ViewData["Title"] = "Dealer Three - AutoVault";
            return View();
        }

        public IActionResult ShopDemo()
        {
            ViewData["Title"] = "Shop Demo - AutoVault";
            return View();
        }

        public IActionResult DealerSlider()
        {
            ViewData["Title"] = "Dealer Slider - AutoVault";
            return View();
        }

        public IActionResult DealerVideo()
        {
            ViewData["Title"] = "Dealer Video - AutoVault";
            return View();
        }

        public IActionResult WashDemo()
        {
            ViewData["Title"] = "Wash Demo - AutoVault";
            return View();
        }

        public IActionResult RepairDemo()
        {
            ViewData["Title"] = "Repair Demo - AutoVault";
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
