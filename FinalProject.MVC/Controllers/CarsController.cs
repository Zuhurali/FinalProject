using Microsoft.AspNetCore.Mvc;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Hubs;
using FinalProject.MVC.Models;
using FinalProject.MVC.DTOs;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Mappings;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace FinalProject.MVC.Controllers
{
    public class CarsController : Controller
    {
        private readonly ICarApiService _carApi;
        private readonly ILogger<CarsController> _logger;
        private readonly IMarketplaceApiService _marketplaceApi;
        private readonly IApiAccessTokenService _tokenService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<ConversationHub> _conversations;

        public CarsController(
            ICarApiService carApi,
            ILogger<CarsController> logger,
            IMarketplaceApiService marketplaceApi,
            IApiAccessTokenService tokenService,
            UserManager<ApplicationUser> userManager,
            IHubContext<ConversationHub> conversations)
        {
            _carApi = carApi;
            _logger = logger;
            _marketplaceApi = marketplaceApi;
            _tokenService = tokenService;
            _userManager = userManager;
            _conversations = conversations;
        }

        public async Task<IActionResult> Index(string? category, string? make, string? model, string? priceRange, string? fuel, string? transmission, string? search, string? condition, int page = 1)
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
                Condition = condition,
                Page = page
            };

            try
            {
                var catalog = await _carApi.GetAllCarsAsync(CarPublicListing.CreateActiveQuery(), HttpContext.RequestAborted);
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
                catalogDtos = await _carApi.GetAllCarsAsync(CarPublicListing.CreateActiveQuery(), HttpContext.RequestAborted);
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
                var catalog = await _carApi.GetAllCarsAsync(CarPublicListing.CreateActiveQuery(), HttpContext.RequestAborted);
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
            var cars = (await LoadCarsAsync())
                .OrderByDescending(car => car.CreatedAt)
                .ThenByDescending(car => car.Id)
                .ToList();
            await MarkFavoritesAsync(cars);
            ViewData["BrandIds"] = await LoadBrandIdsAsync();
            return View(cars);
        }

        [HttpGet]
        public async Task<IActionResult> BrandModels(int brandId)
        {
            if (brandId <= 0)
            {
                return Json(Array.Empty<string>());
            }

            try
            {
                var lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted);
                var names = lookups.Models
                    .Where(model => model.BrandId == brandId && !string.IsNullOrWhiteSpace(model.Name))
                    .Select(model => model.Name.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                return Json(names);
            }
            catch (CarApiException exception)
            {
                _logger.LogWarning(exception, "Models for brand {BrandId} could not be loaded.", brandId);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Models are temporarily unavailable." });
            }
        }

        public async Task<IActionResult> CarsThree(
            string? search,
            string? brand,
            string? model,
            string? type,
            decimal? minPrice,
            decimal? maxPrice,
            int? minYear,
            int? maxYear,
            string? fuel,
            string? transmission,
            int? minMileage,
            int? maxMileage,
            int[]? seats,
            string? sort,
            int page = 1)
        {
            ViewData["Title"] = "Cars Three - AutoVault";
            ViewData["BodyClass"] = "with-sidebar";
            ViewData["HeaderStyle"] = "three";

            var filters = NormalizeCarsThreeFilters(
                search, brand, model, type, minPrice, maxPrice, minYear, maxYear,
                fuel, transmission, minMileage, maxMileage, seats, sort, page);
            var pageModel = await BuildCarsThreePageAsync(filters, includeFacets: true);
            return View(pageModel);
        }

        [HttpGet]
        public async Task<IActionResult> CarsThreeResults(
            string? search,
            string? brand,
            string? model,
            string? type,
            decimal? minPrice,
            decimal? maxPrice,
            int? minYear,
            int? maxYear,
            string? fuel,
            string? transmission,
            int? minMileage,
            int? maxMileage,
            int[]? seats,
            string? sort,
            int page = 1)
        {
            var filters = NormalizeCarsThreeFilters(
                search, brand, model, type, minPrice, maxPrice, minYear, maxYear,
                fuel, transmission, minMileage, maxMileage, seats, sort, page);
            var pageModel = await BuildCarsThreePageAsync(filters, includeFacets: false);
            Response.Headers["X-Cars3-Page"] = pageModel.Page.ToString();
            Response.Headers["X-Cars3-PageSize"] = pageModel.PageSize.ToString();
            Response.Headers["X-Cars3-Total-Count"] = pageModel.TotalCount.ToString();
            Response.Headers["X-Cars3-Total-Pages"] = pageModel.TotalPages.ToString();
            Response.Headers["X-Cars3-Has-More"] = pageModel.HasMore ? "1" : "0";
            Response.Headers["X-Cars3-Ids"] = string.Join(",", pageModel.Cars.Select(car => car.Id));
            if (!string.IsNullOrWhiteSpace(pageModel.FilterError))
            {
                Response.Headers["X-Cars3-Filter-Error"] = pageModel.FilterError;
            }

            return PartialView("_CarsThreeCards", pageModel);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || id <= 0)
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var carDto = await _carApi.GetCarByIdAsync(id.Value, cancellationToken: HttpContext.RequestAborted);
                if (carDto == null || !CarPublicListing.IsPubliclyListed(carDto.Status))
                {
                    return NotFound();
                }

                var cars = await CarApiMapper.LoadCarsAsync(_carApi, HttpContext.RequestAborted);
                var car = CarApiMapper.ToCar(carDto!);
                ViewData["RelatedCars"] = cars
                    .Where(item => item.Id != car.Id)
                    .GroupBy(item => item.Id)
                    .Select(group => group.First())
                    .OrderByDescending(item => item.Category.Equals(car.Category, StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(item => item.Make.Equals(car.Make, StringComparison.OrdinalIgnoreCase))
                    .ThenBy(item => item.Name)
                    .Take(2)
                    .ToList();
                await MarkFavoritesAsync([car]);
                try
                {
                    await _carApi.RecordViewAsync(car.Id, HttpContext.RequestAborted);
                }
                catch (CarApiException exception)
                {
                    _logger.LogWarning(exception, "View for car {CarId} could not be recorded.", car.Id);
                }

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

        public async Task<IActionResult> MostViewed(string? period, string? condition, int page = 1)
        {
            var selected = period?.Trim().ToLowerInvariant() switch
            {
                "today" => "today",
                "week" => "week",
                _ => "all"
            };
            var usedOnly = string.Equals(condition, "used", StringComparison.OrdinalIgnoreCase);
            return await ExplorePageAsync(
                usedOnly ? "Used Cars" : "Most Viewed Cars",
                usedOnly
                    ? "Active used cars, ranked by how many times each car's details page was opened."
                    : "Ranked by how many times each car's details page was opened.",
                "viewed",
                selected,
                "views",
                nameof(MostViewed),
                page,
                async () =>
                {
                    var result = await _carApi.GetMostViewedAsync(
                        selected,
                        page,
                        usedOnly ? CarExplorePage.UsedPageSize : CarExplorePage.PageSize,
                        usedOnly,
                        HttpContext.RequestAborted);
                    return (
                        result.Items.Select(item => new CarExploreItem
                        {
                            Car = CarApiMapper.ToCar(item.Car),
                            Count = item.Count,
                            Detail = $"{item.Count} view{(item.Count == 1 ? "" : "s")}"
                        }).ToList(),
                        result.CurrentPage,
                        result.TotalPages,
                        result.TotalCount);
                },
                usedOnly ? "used" : null);
        }

        public async Task<IActionResult> MostFavorited(int page = 1)
        {
            return await ExplorePageAsync(
                "Most Favorited Cars",
                "Ranked by how many accounts currently have each car saved.",
                "favorited",
                "all",
                "favorites",
                nameof(MostFavorited),
                page,
                async () =>
                {
                    var result = await _carApi.GetMostFavoritedAsync(
                        page,
                        CarExplorePage.PageSize,
                        HttpContext.RequestAborted);
                    return (
                        result.Items.Select(item => new CarExploreItem
                        {
                            Car = CarApiMapper.ToCar(item.Car),
                            Count = item.Count,
                            Detail = item.Count == 1
                                ? "Saved by 1 member"
                                : $"Saved by {item.Count} members"
                        }).ToList(),
                        result.CurrentPage,
                        result.TotalPages,
                        result.TotalCount);
                });
        }

        public async Task<IActionResult> Latest(int page = 1)
        {
            return await ExplorePageAsync(
                "Latest Arrivals",
                "Newest listings first, using each car's listing date.",
                "latest",
                "all",
                "",
                nameof(Latest),
                page,
                async () =>
                {
                    var result = await _carApi.GetLatestArrivalsAsync(
                        page,
                        CarExplorePage.PageSize,
                        HttpContext.RequestAborted);
                    return (
                        result.Items.Select(dto =>
                        {
                            var car = CarApiMapper.ToCar(dto);
                            return new CarExploreItem
                            {
                                Car = car,
                                Detail = $"Listed {car.CreatedAt:dd MMM yyyy}"
                            };
                        }).ToList(),
                        result.CurrentPage,
                        result.TotalPages,
                        result.TotalCount);
                });
        }

        public IActionResult DetailsTwo(int? id)
        {
            return id is > 0
                ? RedirectToAction(nameof(Details), new { id })
                : RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> SoldCar()
        {
            ViewData["Title"] = "Sold Cars - AutoVault";
            try
            {
                var sold = await _carApi.GetAllCarsAsync(
                    CarPublicListing.CreateSoldQuery(),
                    HttpContext.RequestAborted);
                var cars = sold
                    .Select(CarApiMapper.ToCar)
                    .Where(car => string.Equals(car.Status, CarPublicListing.SoldStatus, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(car => car.CreatedAt)
                    .ThenByDescending(car => car.Id)
                    .ToList();
                await MarkFavoritesAsync(cars);
                return View(cars);
            }
            catch (CarApiException exception)
            {
                _logger.LogError(exception, "Sold car listing could not be loaded.");
                ViewData["CarApiUnavailable"] = true;
                return View(new List<Car>());
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> AddFavorite(int id, string? returnUrl = null)
        {
            return SetFavoriteAsync(id, true, returnUrl);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> RemoveFavorite(int id, string? returnUrl = null)
        {
            return SetFavoriteAsync(id, false, returnUrl);
        }

        private async Task<IActionResult> SetFavoriteAsync(int id, bool favorite, string? returnUrl)
        {
            var wantsJson =
                string.Equals(Request.Headers["X-Requested-With"], "fetch", StringComparison.OrdinalIgnoreCase)
                || Request.Headers.Accept.Any(value => value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return wantsJson
                    ? Unauthorized(new { message = "Sign in to save favourites." })
                    : Challenge();
            }

            try
            {
                var token = await CreateAccessTokenAsync(user);
                if (favorite)
                {
                    await _marketplaceApi.AddFavoriteAsync(id, token, HttpContext.RequestAborted);
                }
                else
                {
                    await _marketplaceApi.RemoveFavoriteAsync(id, token, HttpContext.RequestAborted);
                }
            }
            catch (CarApiException exception)
            {
                _logger.LogWarning(exception, "Favourite for car {CarId} could not be updated.", id);
                const string message = "Favourites are temporarily unavailable. Please try again.";
                if (wantsJson)
                {
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message });
                }

                TempData["FavoriteError"] = message;
                return SafeReturn(returnUrl, id);
            }

            return wantsJson
                ? Json(new { carId = id, isFavorite = favorite })
                : SafeReturn(returnUrl, id);
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
                if (conversation.Notification != null)
                {
                    try
                    {
                        await _conversations.Clients
                            .Group(ConversationHub.InboxGroupName(conversation.Notification.UserId))
                            .SendAsync(
                                ConversationHub.NotificationReceivedEvent,
                                conversation.Notification,
                                HttpContext.RequestAborted);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(
                            exception,
                            "Seller notification for conversation {ConversationId} was saved but not broadcast.",
                            conversation.Id);
                    }
                }

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

        private async Task<IReadOnlyDictionary<string, int>> LoadBrandIdsAsync()
        {
            try
            {
                var lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted);
                return lookups.Brands
                    .Where(brand => !string.IsNullOrWhiteSpace(brand.Name))
                    .GroupBy(brand => brand.Name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);
            }
            catch (CarApiException exception)
            {
                _logger.LogWarning(exception, "Brand ids for Cars Two could not be loaded.");
                return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private async Task<List<Car>> LoadCarsAsync()
        {
            var catalog = await LoadCatalogAsync();
            return catalog;
        }

        private static CarsThreeFilterQuery NormalizeCarsThreeFilters(
            string? search,
            string? brand,
            string? model,
            string? type,
            decimal? minPrice,
            decimal? maxPrice,
            int? minYear,
            int? maxYear,
            string? fuel,
            string? transmission,
            int? minMileage,
            int? maxMileage,
            int[]? seats,
            string? sort,
            int page)
        {
            var normalizedSort = string.IsNullOrWhiteSpace(sort) ? "latest" : sort.Trim().ToLowerInvariant();
            if (normalizedSort is not ("latest" or "price-asc" or "price-desc" or "year-desc" or "year-asc"))
            {
                normalizedSort = "latest";
            }

            return new CarsThreeFilterQuery
            {
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim(),
                Model = string.IsNullOrWhiteSpace(model) ? null : model.Trim(),
                Type = string.IsNullOrWhiteSpace(type) ? null : type.Trim(),
                MinPrice = minPrice is >= 0 ? minPrice : null,
                MaxPrice = maxPrice is >= 0 ? maxPrice : null,
                MinYear = minYear is >= 0 ? minYear : null,
                MaxYear = maxYear is >= 0 ? maxYear : null,
                Fuel = string.IsNullOrWhiteSpace(fuel) ? null : fuel.Trim(),
                Transmission = string.IsNullOrWhiteSpace(transmission) ? null : transmission.Trim(),
                MinMileage = minMileage is >= 0 ? minMileage : null,
                MaxMileage = maxMileage is >= 0 ? maxMileage : null,
                Seats = (seats ?? Array.Empty<int>())
                    .Where(seat => seat is >= 1 and <= 12)
                    .Distinct()
                    .OrderBy(seat => seat)
                    .ToList(),
                Sort = normalizedSort,
                Page = page < 1 ? 1 : page
            };
        }

        private async Task<CarsThreePageModel> BuildCarsThreePageAsync(
            CarsThreeFilterQuery filters,
            bool includeFacets)
        {
            var pageModel = new CarsThreePageModel
            {
                Filters = filters,
                Page = filters.Page,
                PageSize = CarsThreeFilterQuery.PageSize,
                Fuels = CarListingRules.Fuels,
                Transmissions = CarListingRules.Transmissions,
                SeatsOptions = CarListingRules.SeatChoices,
                MinPriceBound = 0,
                MaxPriceBound = 500_000,
                MinYearBound = 1990,
                MaxYearBound = DateTime.UtcNow.Year + 1
            };

            try
            {
                var lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted);
                if (includeFacets)
                {
                    pageModel.Makes = lookups.Brands
                        .Where(item => !string.IsNullOrWhiteSpace(item.Name))
                        .Select(item => item.Name.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    pageModel.Models = lookups.Models
                        .Where(item => !string.IsNullOrWhiteSpace(item.Name))
                        .Select(item => item.Name.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    pageModel.Types = lookups.Categories
                        .Where(item => !string.IsNullOrWhiteSpace(item.Name))
                        .Select(item => item.Name.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                pageModel.FilterError = filters.RangeError();
                if (pageModel.FilterError != null)
                {
                    pageModel.Cars = Array.Empty<Car>();
                    pageModel.TotalCount = 0;
                    pageModel.TotalPages = 0;
                    return pageModel;
                }

                if (!TryCreateCarsThreeApiQuery(filters, lookups, out var apiQuery))
                {
                    pageModel.Cars = Array.Empty<Car>();
                    pageModel.TotalCount = 0;
                    pageModel.TotalPages = 0;
                    return pageModel;
                }

                var page = await _carApi.GetCarsAsync(apiQuery, HttpContext.RequestAborted);
                if (page.TotalPages > 0 && apiQuery.Page > page.TotalPages)
                {
                    apiQuery.Page = page.TotalPages;
                    page = await _carApi.GetCarsAsync(apiQuery, HttpContext.RequestAborted);
                }

                var cars = page.Items.Select(CarApiMapper.ToCar).ToList();
                await MarkFavoritesAsync(cars);
                pageModel.Cars = cars;
                pageModel.Page = page.CurrentPage < 1 ? filters.Page : page.CurrentPage;
                pageModel.TotalCount = page.TotalCount;
                pageModel.TotalPages = page.TotalPages;
            }
            catch (CarApiException exception)
            {
                _logger.LogError(exception, "Cars Three listing could not be loaded.");
                pageModel.ApiError = exception.Message;
                pageModel.Cars = Array.Empty<Car>();
            }

            return pageModel;
        }

        private static bool TryCreateCarsThreeApiQuery(
            CarsThreeFilterQuery filters,
            CarLookupDto lookups,
            out CarApiQueryDto apiQuery)
        {
            apiQuery = CarPublicListing.WithActiveStatus(new CarApiQueryDto
            {
                Page = filters.Page < 1 ? 1 : filters.Page,
                PageSize = CarsThreeFilterQuery.PageSize,
                Sort = string.IsNullOrWhiteSpace(filters.Sort) ? "latest" : filters.Sort,
                Search = filters.Search,
                Model = filters.Model,
                MinPrice = filters.MinPrice,
                MaxPrice = filters.MaxPrice,
                MinYear = filters.MinYear,
                MaxYear = filters.MaxYear,
                MinMileage = filters.MinMileage,
                MaxMileage = filters.MaxMileage,
                Seats = filters.Seats.Count == 1 ? filters.Seats[0] : null,
                SeatCounts = filters.Seats.ToList(),
                Fuel = filters.Fuel,
                Transmission = filters.Transmission,
                IncludeListingText = true
            });

            if (filters.MinPrice is decimal minPrice
                && filters.MaxPrice is decimal maxPrice
                && minPrice > maxPrice)
            {
                return false;
            }

            if (filters.MinYear is int minYear
                && filters.MaxYear is int maxYear
                && minYear > maxYear)
            {
                return false;
            }

            if (filters.MinMileage is int minMileage
                && filters.MaxMileage is int maxMileage
                && minMileage > maxMileage)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(filters.Brand))
            {
                var brandId = lookups.Brands
                    .FirstOrDefault(item => string.Equals(item.Name, filters.Brand, StringComparison.OrdinalIgnoreCase))
                    ?.Id;
                if (brandId == null)
                {
                    return false;
                }

                apiQuery.BrandId = brandId;
            }

            if (!string.IsNullOrWhiteSpace(filters.Type))
            {
                var category = lookups.Categories
                    .FirstOrDefault(item => CarCategories.Matches(item.Name, filters.Type));
                if (category == null)
                {
                    return false;
                }

                apiQuery.CategoryId = category.Id;
            }

            return true;
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
            var lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted);
            var categories = lookups.Categories
                .Where(category => !string.IsNullOrWhiteSpace(category.Name))
                .OrderBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var brandId = ResolveBrandId(query.Make, catalog, lookups.Brands);
            if (!TryCreateApiQuery(query, categories, CarListingResult.PageSize, brandId, out var apiQuery))
            {
                return ListingFrom(query, catalogCars, Array.Empty<Car>(), query.Page < 1 ? 1 : query.Page, 0, 0, categories);
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
                page.TotalPages,
                categories);
        }

        private async Task<List<Car>> LoadMatchingCarsAsync(CarListingQuery query, IReadOnlyList<CarApiDto> catalogDtos)
        {
            var lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted);
            var brandId = ResolveBrandId(query.Make, catalogDtos, lookups.Brands);
            if (!TryCreateApiQuery(query, lookups.Categories, CarApiQueryDto.MaxPageSize, brandId, out var apiQuery))
            {
                return new List<Car>();
            }

            var matches = await _carApi.GetAllCarsAsync(apiQuery, HttpContext.RequestAborted);
            return matches.Select(CarApiMapper.ToCar).ToList();
        }

        private static int? ResolveBrandId(
            string? make,
            IReadOnlyList<CarApiDto> catalog,
            IReadOnlyList<BrandApiDto> brands)
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

            return brands.FirstOrDefault(brand => string.Equals(brand.Name, make, StringComparison.OrdinalIgnoreCase))?.Id;
        }

        private static bool TryCreateApiQuery(
            CarListingQuery query,
            IReadOnlyList<CategoryApiDto> categories,
            int pageSize,
            int? brandId,
            out CarApiQueryDto apiQuery)
        {
            apiQuery = CarPublicListing.WithActiveStatus(new CarApiQueryDto
            {
                Page = query.Page < 1 ? 1 : query.Page,
                PageSize = pageSize
            });

            if (!string.IsNullOrWhiteSpace(query.Category))
            {
                var category = categories.FirstOrDefault(item => CarCategories.Matches(item.Name, query.Category));
                if (category == null)
                {
                    return false;
                }

                apiQuery.CategoryId = category.Id;
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

            if (string.Equals(query.Condition, "new", StringComparison.OrdinalIgnoreCase))
            {
                apiQuery.IsNew = true;
            }
            else if (string.Equals(query.Condition, "used", StringComparison.OrdinalIgnoreCase))
            {
                apiQuery.IsNew = false;
            }

            return true;
        }

        private static CarListingResult ListingFrom(
            CarListingQuery query,
            IReadOnlyList<Car> catalog,
            IReadOnlyList<Car> cars,
            int page,
            int totalCount,
            int totalPages,
            IReadOnlyList<CategoryApiDto> categories)
        {
            var matched = categories.FirstOrDefault(item => CarCategories.Matches(item.Name, query.Category));
            var display = matched?.Name
                ?? (string.IsNullOrWhiteSpace(query.Category) ? null : query.Category.Trim().Replace('-', ' '));
            return new CarListingResult
            {
                Cars = cars,
                Query = query,
                Page = page,
                TotalCount = totalCount,
                TotalPages = totalPages,
                CategoryRequested = !string.IsNullOrWhiteSpace(query.Category),
                CategorySlug = string.IsNullOrWhiteSpace(display) ? query.Category : CarCategories.Slug(display),
                CategoryDisplay = display ?? query.Category,
                Makes = catalog.Select(car => car.Make).Where(make => !string.IsNullOrWhiteSpace(make)).Distinct().OrderBy(make => make).ToList(),
                Models = catalog.Select(car => car.Model).Where(model => !string.IsNullOrWhiteSpace(model)).Distinct().OrderBy(model => model).ToList(),
                Categories = categories
            };
        }

        private static bool HasFilterValue(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) && !value.Equals("All", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<IActionResult> ExplorePageAsync(
            string heading,
            string intro,
            string kind,
            string period,
            string countLabel,
            string actionName,
            int pageNumber,
            Func<Task<(List<CarExploreItem> Cars, int CurrentPage, int TotalPages, int TotalCount)>> load,
            string? condition = null)
        {
            ViewData["Title"] = $"{heading} - AutoVault";
            var page = new CarExplorePage
            {
                Heading = heading,
                Intro = intro,
                Kind = kind,
                Period = period,
                CountLabel = countLabel,
                ActionName = actionName,
                Condition = string.Equals(condition, "used", StringComparison.OrdinalIgnoreCase) ? "used" : null,
                Page = pageNumber < 1 ? 1 : pageNumber
            };
            try
            {
                var loaded = await load();
                page.Cars = loaded.Cars;
                page.Page = loaded.CurrentPage < 1 ? 1 : loaded.CurrentPage;
                page.TotalPages = loaded.TotalPages;
                page.TotalCount = loaded.TotalCount;
                await MarkFavoritesAsync(page.Cars.Select(item => item.Car));
            }
            catch (CarApiException exception)
            {
                _logger.LogError(exception, "{Heading} could not be loaded.", heading);
                page.ApiUnavailable = true;
            }

            return View("Explore", page);
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
