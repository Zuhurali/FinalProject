using Microsoft.AspNetCore.Mvc;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;

namespace FinalProject.MVC.Controllers
{
    public class CarsController : Controller
    {
        private readonly List<Car> _allCars = CarCatalog.Create();
        private readonly List<Car> _templateCars = CarCatalog.TemplateCards();

        public IActionResult Index(string? category, string? make, string? model, string? priceRange, string? fuel, string? transmission, int page = 1)
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

            var matches = CarListingFilter.Filter(_allCars, query);
            var listing = CarListingFilter.Page(matches, query, _allCars);
            return View(listing);
        }

        public IActionResult CarsOne(string? make, string? model, string? priceRange)
        {
            ViewData["Title"] = "Cars One - AutoVault";

            var filteredCars = CarListingFilter.Filter(_templateCars, new CarListingQuery
            {
                Make = make,
                Model = model,
                PriceRange = priceRange
            }).ToList();

            // Get distinct makes and models for dropdowns
            ViewBag.Makes = _templateCars.Select(c => c.Make).Distinct().OrderBy(m => m).ToList();
            ViewBag.Models = _templateCars.Select(c => c.Model).Distinct().OrderBy(m => m).ToList();
            ViewBag.SelectedMake = make;
            ViewBag.SelectedModel = model;
            ViewBag.SelectedPriceRange = priceRange;

            return View(filteredCars);
        }

        [HttpPost]
        public JsonResult FilterCars([FromBody] CarFilterRequest request)
        {
            var filteredCars = CarListingFilter.Filter(_templateCars, new CarListingQuery
            {
                Category = request.Category,
                Make = request.Make,
                Model = request.Model,
                PriceRange = request.PriceRange,
                Fuel = request.Fuel,
                Transmission = request.Transmission
            });

            return Json(new CarFilterResponse
            {
                Cars = filteredCars.ToList(),
                TotalCars = filteredCars.Count,
                ResultsText = $"Showing {filteredCars.Count} car(s)"
            });
        }

        public IActionResult CarsTwo()
        {
            ViewData["Title"] = "Cars Two - AutoVault";
            ViewData["BodyClass"] = "with-sidebar";
            ViewData["HeaderStyle"] = "three";
            return View(_templateCars);
        }

        public IActionResult CarsThree()
        {
            ViewData["Title"] = "Cars Three - AutoVault";
            ViewData["BodyClass"] = "with-sidebar";
            ViewData["HeaderStyle"] = "three";
            return View();
        }

        public IActionResult Details(int? id)
        {
            if (id == null)
            {
                return RedirectToAction(nameof(CarsOne));
            }

            var car = _allCars.FirstOrDefault(c => c.Id == id.Value);
            if (car == null)
            {
                TempData["Error"] = "Car not found";
                return RedirectToAction(nameof(CarsOne));
            }

            ViewData["Title"] = $"{car.Name} - AutoVault";
            ViewData["BodyClass"] = "with-sidebar";
            ViewData["HeaderStyle"] = "three";
            return View(car);
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
    }
}
