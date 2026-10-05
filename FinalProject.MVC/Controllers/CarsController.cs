using Microsoft.AspNetCore.Mvc;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Controllers
{
    public class CarsController : Controller
    {
        private readonly ICarApiService _carApi;

        public CarsController(ICarApiService carApi)
        {
            _carApi = carApi;
        }

        public async Task<IActionResult> Index(string? category, string? make, string? model, string? priceRange, string? fuel, string? transmission, int page = 1)
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
                Page = page
            };

            try
            {
                var catalog = await _carApi.GetAllCarsAsync(cancellationToken: HttpContext.RequestAborted);
                return View(await BuildListingAsync(query, catalog));
            }
            catch (CarApiException exception)
            {
                var listing = CarListingFilter.Page(Array.Empty<Car>(), query, Array.Empty<Car>());
                listing.ErrorMessage = exception.Message;
                return View(listing);
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
            return View(await LoadCarsAsync());
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
                ViewData["Title"] = $"{car.Name} - AutoVault";
                ViewData["BodyClass"] = "with-sidebar";
                ViewData["HeaderStyle"] = "three";
                return View(car);
            }
            catch (CarApiException exception)
            {
                TempData["Error"] = exception.Message;
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
            if (!TryCreateApiQuery(query, catalog, CarListingResult.PageSize, out var apiQuery))
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
            if (!TryCreateApiQuery(query, catalogDtos, CarApiQuery.MaxPageSize, out var apiQuery))
            {
                return new List<Car>();
            }

            var matches = await _carApi.GetAllCarsAsync(apiQuery, HttpContext.RequestAborted);
            return matches.Select(CarApiMapper.ToCar).ToList();
        }

        private static bool TryCreateApiQuery(CarListingQuery query, IReadOnlyList<CarApiDto> catalog, int pageSize, out CarApiQuery apiQuery)
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
                var brandId = catalog.FirstOrDefault(car => string.Equals(car.Brand?.Name, query.Make, StringComparison.OrdinalIgnoreCase))?.Brand?.Id;
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
    }
}
