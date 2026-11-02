using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
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
        private readonly ILogger<HomeController> _logger;
        private readonly ICarApiService _carApi;
        private readonly IShopCatalogService _shopCatalog;
        private readonly ILegalPageContentService _legalPage;

        public HomeController(
            ILogger<HomeController> logger,
            ICarApiService carApi,
            IShopCatalogService shopCatalog,
            ILegalPageContentService legalPage)
        {
            _logger = logger;
            _carApi = carApi;
            _shopCatalog = shopCatalog;
            _legalPage = legalPage;
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
            }
            catch (CarApiException exception)
            {
                // Do not serve a process-local snapshot as inventory — SQL/API is the source of truth.
                _logger.LogWarning(exception, "Home car catalog could not be loaded.");
                cars = Array.Empty<Car>();
                ViewData["CarApiUnavailable"] = true;
            }

            makes = cars.Select(car => car.Make).Where(make => !string.IsNullOrWhiteSpace(make)).Distinct().OrderBy(make => make).ToList();
            modelsByMake = cars
                .Where(car => !string.IsNullOrWhiteSpace(car.Make) && !string.IsNullOrWhiteSpace(car.Model))
                .GroupBy(car => car.Make)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(car => car.Model).Distinct().OrderBy(model => model).ToArray());

            IReadOnlyList<BrandApiDto> brands = Array.Empty<BrandApiDto>();
            IReadOnlyList<HomeCategoryStripItem> homeCategories = Array.Empty<HomeCategoryStripItem>();
            try
            {
                var lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted);
                brands = lookups.Brands;
                homeCategories = lookups.Categories
                    .Where(category => !string.IsNullOrWhiteSpace(category.Name))
                    .OrderBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
                    .Select((category, index) =>
                    {
                        var name = category.Name.Trim();
                        labels[name] = cars.Count == 0 ? "—" : CarApiMapper.CountLabel(cars, name);
                        return new HomeCategoryStripItem
                        {
                            Name = name,
                            Slug = CarCategories.Slug(name),
                            IconPath = CategoryIconPaths.Resolve(category.IconPath, index),
                            CountLabel = labels[name]
                        };
                    })
                    .ToList();
            }
            catch (CarApiException exception)
            {
                _logger.LogWarning(exception, "Home brand and category lists could not be loaded.");
            }

            IReadOnlyList<ProductCampaign> homeCampaignBanners = Array.Empty<ProductCampaign>();
            try
            {
                homeCampaignBanners = CampaignBannerSelection.ForHome(
                    await _shopCatalog.GetLiveCampaignsAsync(HttpContext.RequestAborted));
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Home campaign banners could not be loaded.");
            }

            ViewData["CategoryLabels"] = labels;
            ViewData["HomeCategories"] = homeCategories;
            ViewData["SearchMakes"] = makes;
            ViewData["ModelsByMake"] = modelsByMake;
            ViewData["HomeCars"] = cars;
            ViewData["HomeBrands"] = brands;
            ViewData["LiveCampaigns"] = homeCampaignBanners;
            return View();
        }

        public IActionResult DealerTwo()
        {
            return RedirectToActionPermanent(nameof(Index));
        }

        public IActionResult DealerThree()
        {
            return RedirectToActionPermanent(nameof(Index));
        }

        public IActionResult ShopDemo()
        {
            return RedirectToActionPermanent("Index", "Shop");
        }

        public IActionResult DealerSlider()
        {
            return RedirectToActionPermanent(nameof(Index));
        }

        public IActionResult DealerVideo()
        {
            return RedirectToActionPermanent(nameof(Index));
        }

        public IActionResult WashDemo()
        {
            return RedirectToActionPermanent("Index", "Contact");
        }

        public IActionResult RepairDemo()
        {
            return RedirectToActionPermanent("Index", "Contact");
        }

        public IActionResult Privacy()
        {
            var legal = _legalPage.GetPublic();
            ViewData["Title"] = legal.PrivacySeoTitle;
            ViewData["MetaDescription"] = legal.PrivacySeoDescription;
            ViewData["Legal"] = legal;
            return View();
        }

        public IActionResult Terms()
        {
            var legal = _legalPage.GetPublic();
            ViewData["Title"] = legal.TermsSeoTitle;
            ViewData["MetaDescription"] = legal.TermsSeoDescription;
            ViewData["Legal"] = legal;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
