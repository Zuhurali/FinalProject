using Microsoft.AspNetCore.Mvc;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace FinalProject.MVC.Controllers
{
    public class CarsController : Controller
    {
        private readonly ICarApiService _carApi;
        private readonly ILogger<CarsController> _logger;
        private readonly IMarketplaceApiService _marketplaceApi;
        private readonly IApiAccessTokenService _tokenService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CarsController(
            ICarApiService carApi,
            ILogger<CarsController> logger,
            IMarketplaceApiService marketplaceApi,
            IApiAccessTokenService tokenService,
            UserManager<ApplicationUser> userManager)
        {
            _carApi = carApi;
            _logger = logger;
            _marketplaceApi = marketplaceApi;
            _tokenService = tokenService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? category, string? make, string? model, string? priceRange, string? fuel, string? transmission, string? search, int page = 1)
        {
            ViewData["Title"] = "Cars - AutoVault";

            var query = new CarListingQuery
            {
                Category = category,
                Make = make,
                Model = model,
                PriceRange = priceRange,
                Fuel = fuel,
                Transmission = transmission,
                Search = search,
                Page = page
            };

            try
            {
                var catalog = await _carApi.GetAllCarsAsync(cancellationToken: HttpContext.RequestAborted);
                var listing = await BuildListingAsync(query, catalog);
                await MarkFavoritesAsync(listing.Cars);
                return View(listing);
            }
            catch (CarApiException exception)
            {
                _logger.LogError(exception, "Car listing could not be loaded.");
                var listing = CarListingFilter.Page(Array.Empty<Car>(), query, Array.Empty<Car>());
                listing.ApiUnavailable = true;
                return View(listing);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Suggestions(string? query, int limit = 8)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            {
                return Json(Array.Empty<CarSuggestionDto>());
            }

            try
            {
                var suggestions = await _carApi.GetSuggestionsAsync(query.Trim(), limit, HttpContext.RequestAborted);
                return Json(suggestions);
            }
            catch (CarApiException exception)
            {
                _logger.LogWarning(exception, "Car suggestions could not be loaded.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Cars are temporarily unavailable." });
            }
        }

        public async Task<IActionResult> CarsOne(string? make, string? model, string? priceRange)
        {
            ViewData["Title"] = "Cars One - AutoVault";
            IReadOnlyList<CarApiDto> catalogDtos = Array.Empty<CarApiDto>();
            try
            {
                catalogDtos = await _carApi.GetAllCarsAsync(cancellationToken: HttpContext.RequestAborted);
            }
            catch (CarApiException exception)
            {
                ViewData["CarApiError"] = exception.Message;
            }

            var catalog = catalogDtos.Select(CarApiMapper.ToCar).ToList();
            var filteredCars = ViewData["CarApiError"] == null
                ? await LoadMatchingCarsAsync(new CarListingQuery
                {
                    Make = make,
                    Model = model,
                    PriceRange = priceRange
                }, catalogDtos)
                : new List<Car>();
            await MarkFavoritesAsync(filteredCars);

            ViewBag.Makes = catalog.Select(c => c.Make).Distinct().OrderBy(m => m).ToList();
            ViewBag.Models = catalog.Select(c => c.Model).Distinct().OrderBy(m => m).ToList();
            ViewBag.SelectedMake = make;
            ViewBag.SelectedModel = model;
            ViewBag.SelectedPriceRange = priceRange;

            return View(filteredCars);
        }

        [HttpPost]
        public async Task<JsonResult> FilterCars([FromBody] CarFilterRequest request)
        {
            try
            {
                var catalog = await _carApi.GetAllCarsAsync(cancellationToken: HttpContext.RequestAborted);
                var filteredCars = await LoadMatchingCarsAsync(new CarListingQuery
                {
                    Category = request.Category,
                    Make = request.Make,
                    Model = request.Model,
                    PriceRange = request.PriceRange,
                    Fuel = request.Fuel,
                    Transmission = request.Transmission
                }, catalog);

                return Json(new CarFilterResponse
                {
                    Cars = filteredCars,
                    TotalCars = filteredCars.Count,
                    ResultsText = $"Showing {filteredCars.Count} car(s)"
                });
            }
            catch (CarApiException exception)
            {
                return Json(new CarFilterResponse
                {
                    ResultsText = exception.Message
                });
            }
        }

        public async Task<IActionResult> CarsTwo()
        {
            ViewData["Title"] = "Cars Two - AutoVault";
            ViewData["BodyClass"] = "with-sidebar";
            ViewData["HeaderStyle"] = "three";
            var cars = await LoadCarsAsync();
            await MarkFavoritesAsync(cars);
            return View(cars);
        }

        public IActionResult CarsThree()
        {
            ViewData["Title"] = "Cars Three - AutoVault";
            ViewData["BodyClass"] = "with-sidebar";
            ViewData["HeaderStyle"] = "three";
            return View();
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || id <= 0)
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var carDto = await _carApi.GetCarByIdAsync(id.Value, HttpContext.RequestAborted);
                if (carDto == null)
                {
                    TempData["Error"] = "Car not found";
                    return RedirectToAction(nameof(Index));
                }

                var cars = await CarApiMapper.LoadCarsAsync(_carApi, HttpContext.RequestAborted);
                ViewData["RelatedCars"] = cars
                    .Where(car => car.Id != carDto.Id)
                    .Take(2)
                    .ToList();

                var car = CarApiMapper.ToCar(carDto);
                await MarkFavoritesAsync([car]);
                ViewData["Title"] = $"{car.Name} - AutoVault";
                ViewData["BodyClass"] = "with-sidebar";
                ViewData["HeaderStyle"] = "three";
                return View(car);
            }
            catch (CarApiException exception)
            {
                _logger.LogError(exception, "Car {CarId} could not be loaded.", id);
                TempData["CarApiUnavailable"] = true;
                return RedirectToAction(nameof(Index));
            }
        }

        public IActionResult DetailsTwo(int? id)
        {
            ViewData["Title"] = "Car Single Two - AutoVault";
            ViewData["BodyClass"] = "with-sidebar";
            ViewData["HeaderStyle"] = "three";
            ViewData["CarId"] = id ?? 1;
            return View();
        }

        public IActionResult SoldCar()
        {
            ViewData["Title"] = "Sold Car - AutoVault";
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFavorite(int id, string? returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            await _marketplaceApi.AddFavoriteAsync(
                id,
                await CreateAccessTokenAsync(user),
                HttpContext.RequestAborted);
            return SafeReturn(returnUrl, id);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFavorite(int id, string? returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            await _marketplaceApi.RemoveFavoriteAsync(
                id,
                await CreateAccessTokenAsync(user),
                HttpContext.RequestAborted);
            return SafeReturn(returnUrl, id);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ContactSeller(
            int id,
            string? subject,
            string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Enter a message for the seller.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            try
            {
                var conversation = await _marketplaceApi.StartConversationAsync(
                    id,
                    subject,
                    message,
                    await CreateAccessTokenAsync(user),
                    HttpContext.RequestAborted);
                return RedirectToAction(
                    "Conversation",
                    "Account",
                    new { id = conversation.Id });
            }
            catch (CarApiException exception)
            {
                TempData["Error"] = exception.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        private async Task<List<Car>> LoadCarsAsync()
        {
            var catalog = await LoadCatalogAsync();
            return catalog;
        }

        private async Task<List<Car>> LoadCatalogAsync()
        {
            try
            {
                return (await CarApiMapper.LoadCarsAsync(_carApi, HttpContext.RequestAborted)).ToList();
            }
            catch (CarApiException exception)
            {
                ViewData["CarApiError"] = exception.Message;
                return new List<Car>();
            }
        }

        private async Task<CarListingResult> BuildListingAsync(CarListingQuery query, IReadOnlyList<CarApiDto> catalog)
        {
            var catalogCars = catalog.Select(CarApiMapper.ToCar).ToList();
            var brandId = await ResolveBrandIdAsync(query.Make, catalog);
            if (!TryCreateApiQuery(query, catalog, CarListingResult.PageSize, brandId, out var apiQuery))
            {
                return ListingFrom(query, catalogCars, Array.Empty<Car>(), query.Page < 1 ? 1 : query.Page, 0, 0);
            }

            var page = await _carApi.GetCarsAsync(apiQuery, HttpContext.RequestAborted);
            if (page.TotalPages > 0 && apiQuery.Page > page.TotalPages)
            {
                apiQuery.Page = page.TotalPages;
                page = await _carApi.GetCarsAsync(apiQuery, HttpContext.RequestAborted);
            }

            return ListingFrom(
                query,
                catalogCars,
                page.Items.Select(CarApiMapper.ToCar).ToList(),
                page.CurrentPage,
                page.TotalCount,
                page.TotalPages);
        }

        private async Task<List<Car>> LoadMatchingCarsAsync(CarListingQuery query, IReadOnlyList<CarApiDto> catalogDtos)
        {
            var brandId = await ResolveBrandIdAsync(query.Make, catalogDtos);
            if (!TryCreateApiQuery(query, catalogDtos, CarApiQuery.MaxPageSize, brandId, out var apiQuery))
            {
                return new List<Car>();
            }

            var matches = await _carApi.GetAllCarsAsync(apiQuery, HttpContext.RequestAborted);
            return matches.Select(CarApiMapper.ToCar).ToList();
        }

        private async Task<int?> ResolveBrandIdAsync(string? make, IReadOnlyList<CarApiDto> catalog)
        {
            if (string.IsNullOrWhiteSpace(make) || make == "All Makes")
            {
                return null;
            }

            var fromCatalog = catalog.FirstOrDefault(car => string.Equals(car.Brand?.Name, make, StringComparison.OrdinalIgnoreCase))?.Brand?.Id;
            if (fromCatalog != null)
            {
                return fromCatalog;
            }

            var lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted);
            return lookups.Brands.FirstOrDefault(brand => string.Equals(brand.Name, make, StringComparison.OrdinalIgnoreCase))?.Id;
        }

        private static bool TryCreateApiQuery(CarListingQuery query, IReadOnlyList<CarApiDto> catalog, int pageSize, int? brandId, out CarApiQuery apiQuery)
        {
            apiQuery = new CarApiQuery
            {
                Page = query.Page < 1 ? 1 : query.Page,
                PageSize = pageSize
            };

            if (!string.IsNullOrWhiteSpace(query.Category))
            {
                var category = CarCategories.Resolve(query.Category);
                var categoryId = category == null
                    ? null
                    : catalog.FirstOrDefault(car => string.Equals(car.Category?.Name, category.Value, StringComparison.OrdinalIgnoreCase))?.Category?.Id;
                if (categoryId == null)
                {
                    return false;
                }

                apiQuery.CategoryId = categoryId;
            }

            if (HasFilterValue(query.Make))
            {
                if (brandId == null)
                {
                    return false;
                }

                apiQuery.BrandId = brandId;
            }

            if (HasFilterValue(query.Model))
            {
                apiQuery.Model = query.Model;
            }

            if (HasFilterValue(query.PriceRange))
            {
                var (minPrice, maxPrice) = CarListingFilter.ParsePriceRange(query.PriceRange!);
                apiQuery.MinPrice = minPrice;
                if (maxPrice < decimal.MaxValue)
                {
                    apiQuery.MaxPrice = maxPrice;
                }
            }

            if (HasFilterValue(query.Fuel))
            {
                apiQuery.Fuel = query.Fuel;
            }

            if (HasFilterValue(query.Transmission))
            {
                apiQuery.Transmission = query.Transmission;
            }

            if (HasFilterValue(query.Search))
            {
                apiQuery.Search = query.Search!.Trim();
            }

            return true;
        }

        private static CarListingResult ListingFrom(
            CarListingQuery query,
            IReadOnlyList<Car> catalog,
            IReadOnlyList<Car> cars,
            int page,
            int totalCount,
            int totalPages)
        {
            var requestedCategory = CarCategories.Resolve(query.Category);
            return new CarListingResult
            {
                Cars = cars,
                Query = query,
                Page = page,
                TotalCount = totalCount,
                TotalPages = totalPages,
                CategoryRequested = !string.IsNullOrWhiteSpace(query.Category),
                CategorySlug = requestedCategory?.Slug ?? query.Category,
                CategoryDisplay = requestedCategory?.DisplayName ?? query.Category,
                Makes = catalog.Select(car => car.Make).Where(make => !string.IsNullOrWhiteSpace(make)).Distinct().OrderBy(make => make).ToList(),
                Models = catalog.Select(car => car.Model).Where(model => !string.IsNullOrWhiteSpace(model)).Distinct().OrderBy(model => model).ToList()
            };
        }

        private static bool HasFilterValue(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) && !value.Equals("All", StringComparison.OrdinalIgnoreCase);
        }

        private async Task MarkFavoritesAsync(IEnumerable<Car> cars)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return;
            }

            try
            {
                var ids = (await _marketplaceApi.GetFavoriteIdsAsync(
                    await CreateAccessTokenAsync(user),
                    HttpContext.RequestAborted)).ToHashSet();
                foreach (var car in cars)
                {
                    car.IsFavorite = ids.Contains(car.Id);
                }
            }
            catch (CarApiException)
            {
                ViewData["FavoriteServiceUnavailable"] = true;
            }
        }

        private async Task<string> CreateAccessTokenAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return _tokenService.CreateToken(User, roles);
        }

        private IActionResult SafeReturn(string? returnUrl, int carId)
        {
            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction(nameof(Details), new { id = carId });
        }
    }
}
