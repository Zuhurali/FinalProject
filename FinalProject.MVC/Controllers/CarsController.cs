using Microsoft.AspNetCore.Mvc;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
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
                var cars = (await _carApi.GetCarsAsync(HttpContext.RequestAborted))
                    .Select(CarApiMapper.ToCar)
                    .ToList();
                var matches = CarListingFilter.Filter(cars, query);
                return View(CarListingFilter.Page(matches, query, cars));
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
            var cars = await LoadCarsAsync();

            var filteredCars = CarListingFilter.Filter(cars, new CarListingQuery
            {
                Make = make,
                Model = model,
                PriceRange = priceRange
            }).ToList();

            ViewBag.Makes = cars.Select(c => c.Make).Distinct().OrderBy(m => m).ToList();
            ViewBag.Models = cars.Select(c => c.Model).Distinct().OrderBy(m => m).ToList();
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
                var cars = await CarApiMapper.LoadCarsAsync(_carApi, HttpContext.RequestAborted);
                var filteredCars = CarListingFilter.Filter(cars, new CarListingQuery
                {
                    Category = request.Category,
                    Make = request.Make,
                    Model = request.Model,
                    PriceRange = request.PriceRange,
                    Fuel = request.Fuel,
                    Transmission = request.Transmission
                }).ToList();

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

                var cars = await _carApi.GetCarsAsync(HttpContext.RequestAborted);
                ViewData["RelatedCars"] = cars
                    .Where(car => car.Id != carDto.Id)
                    .Take(2)
                    .Select(CarApiMapper.ToCar)
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
    }
}
