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
        private readonly IShopCatalogService _shopCatalog;
        private readonly IProductRatingApiService _ratingApi;

        public HomeController(
            ILogger<HomeController> logger,
            ICarApiService carApi,
            IShopCatalogService shopCatalog,
            IProductRatingApiService ratingApi)
        {
            _logger = logger;
            _carApi = carApi;
            _shopCatalog = shopCatalog;
            _ratingApi = ratingApi;
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

        public async Task<IActionResult> DealerThree()
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
                _logger.LogWarning(exception, "Dealer Three catalog could not be loaded.");
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

            ViewData["Title"] = "Dealer Three - AutoVault";
            ViewData["HeaderStyle"] = "three";
            ViewData["DealerCars"] = cars;
            ViewData["SearchMakes"] = makes;
            ViewData["ModelsByMake"] = modelsByMake;
            return View();
        }

        public async Task<IActionResult> ShopDemo(CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Shop - AutoVault";
            ViewData["BodyClass"] = "home-four";
            var products = (await _shopCatalog.GetAllAsync(cancellationToken)).ToList();
            try
            {
                var summaries = await _ratingApi.GetSummariesAsync(products.Select(product => product.Id), cancellationToken);
                var byProduct = summaries.ToDictionary(summary => summary.ProductId);
                foreach (var product in products)
                {
                    if (byProduct.TryGetValue(product.Id, out var summary))
                    {
                        product.Rating = summary.AverageRating;
                        product.RatingCount = summary.TotalCount;
                    }
                }
            }
            catch (CarApiException)
            {
                ViewData["RatingServiceUnavailable"] = true;
            }

            var now = DateTime.UtcNow;
            var campaigns = await _shopCatalog.GetCampaignsAsync(cancellationToken);
            ViewData["ShopProducts"] = products;
            ViewData["FeaturedCampaign"] = campaigns.FirstOrDefault(campaign =>
                campaign.IsActive && campaign.StartsAt <= now && campaign.EndsAt > now);
            return View();
        }

        public IActionResult DealerSlider()
        {
            return RedirectToAction(nameof(Index));
        }

        public IActionResult DealerVideo()
        {
            return RedirectToAction(nameof(Index));
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
