using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FinalProject.MVC.Controllers
{
    public class HomeController : Controller
    {
        private static readonly string[] HomeCategories =
        {
            "Hatchback",
            "Minivans",
            "Luxury Cars",
            "Sedans",
            "Convertible",
            "Sports Car"
        };

        private readonly ILogger<HomeController> _logger;
        private readonly ICarApiService _carApi;

        public HomeController(ILogger<HomeController> logger, ICarApiService carApi)
        {
            _logger = logger;
            _carApi = carApi;
        }

        public async Task<IActionResult> Index()
        {
            var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var cars = await CarApiMapper.LoadCarsAsync(_carApi, HttpContext.RequestAborted);
                foreach (var category in HomeCategories)
                {
                    labels[category] = CarApiMapper.CountLabel(cars, category);
                }
            }
            catch (CarApiException exception)
            {
                ViewData["CarApiError"] = exception.Message;
                foreach (var category in HomeCategories)
                {
                    labels[category] = "Unavailable";
                }
            }

            ViewData["CategoryLabels"] = labels;
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
