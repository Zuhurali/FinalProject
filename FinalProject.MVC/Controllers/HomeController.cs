using FinalProject.MVC.Models;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Mappings;
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

        private static readonly object CatalogLock = new();
        private static IReadOnlyList<Car>? CatalogCache;

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
            IReadOnlyList<string> makes = Array.Empty<string>();
            IReadOnlyDictionary<string, string[]> modelsByMake = new Dictionary<string, string[]>();
            IReadOnlyList<Car> cars;
            try
            {
                cars = await CarApiMapper.LoadCarsAsync(_carApi, HttpContext.RequestAborted);
                lock (CatalogLock)
                {
                    CatalogCache = cars;
                }
            }
            catch (CarApiException exception)
            {
                _logger.LogWarning(exception, "Home car catalog could not be loaded.");
                lock (CatalogLock)
                {
                    cars = CatalogCache ?? Array.Empty<Car>();
                }
            }

            foreach (var category in HomeCategories)
            {
                labels[category] = cars.Count == 0 ? "—" : CarApiMapper.CountLabel(cars, category);
            }

            makes = cars.Select(car => car.Make).Where(make => !string.IsNullOrWhiteSpace(make)).Distinct().OrderBy(make => make).ToList();
            modelsByMake = cars
                .Where(car => !string.IsNullOrWhiteSpace(car.Make) && !string.IsNullOrWhiteSpace(car.Model))
                .GroupBy(car => car.Make)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(car => car.Model).Distinct().OrderBy(model => model).ToArray());

            IReadOnlyList<BrandApiDto> brands = Array.Empty<BrandApiDto>();
            try
            {
                brands = (await _carApi.GetLookupsAsync(HttpContext.RequestAborted)).Brands;
            }
            catch (CarApiException exception)
            {
                _logger.LogWarning(exception, "Home brand list could not be loaded.");
            }

            ViewData["CategoryLabels"] = labels;
            ViewData["SearchMakes"] = makes;
            ViewData["ModelsByMake"] = modelsByMake;
            ViewData["HomeCars"] = cars;
            ViewData["HomeBrands"] = brands;
            return View();
        }

        public async Task<IActionResult> DealerTwo()
        {
            IReadOnlyList<Car> cars;
            try
            {
                cars = await CarApiMapper.LoadCarsAsync(_carApi, HttpContext.RequestAborted);
                lock (CatalogLock)
                {
                    CatalogCache = cars;
                }
            }
            catch (CarApiException exception)
            {
                _logger.LogWarning(exception, "Dealer Two catalog could not be loaded.");
                lock (CatalogLock)
                {
                    cars = CatalogCache ?? Array.Empty<Car>();
                }
            }

            var makes = cars.Select(car => car.Make).Where(make => !string.IsNullOrWhiteSpace(make)).Distinct().OrderBy(make => make).ToList();
            var modelsByMake = cars
                .Where(car => !string.IsNullOrWhiteSpace(car.Make) && !string.IsNullOrWhiteSpace(car.Model))
                .GroupBy(car => car.Make)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(car => car.Model).Distinct().OrderBy(model => model).ToArray());

            ViewData["Title"] = "Dealer Two - AutoVault";
            ViewData["BodyClass"] = "home-two";
            ViewData["DealerCars"] = cars;
            ViewData["SearchMakes"] = makes;
            ViewData["ModelsByMake"] = modelsByMake;
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
