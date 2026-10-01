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
            ViewData["BodyClass"] = "home-two";
            return View();
        }

        public IActionResult DealerThree()
        {
            ViewData["Title"] = "Dealer Three - AutoVault";
            ViewData["HeaderStyle"] = "three";
            return View();
        }

        public IActionResult ShopDemo()
        {
            ViewData["Title"] = "Shop Demo - AutoVault";
            ViewData["BodyClass"] = "home-four";
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
            ViewData["HeaderStyle"] = "five";
            ViewData["FooterStyle"] = "wash";
            return View();
        }

        public IActionResult RepairDemo()
        {
            ViewData["Title"] = "Repair Demo - AutoVault";
            ViewData["HeaderStyle"] = "six";
            ViewData["FooterStyle"] = "repair";
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
