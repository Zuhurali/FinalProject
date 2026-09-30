using Microsoft.AspNetCore.Mvc;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;

namespace FinalProject.MVC.Controllers
{
    public class CarsController : Controller
    {
        private readonly List<Car> _allCars = CarCatalog.Create();

        public IActionResult Index()
        {
            ViewData["Title"] = "Cars - AutoVault";
            return View();
        }

        public IActionResult CarsOne(string? make, string? model, string? priceRange)
        {
            ViewData["Title"] = "Cars One - AutoVault";

            var filteredCars = FilterCars(make, model, priceRange);

            // Get distinct makes and models for dropdowns
            ViewBag.Makes = _allCars.Select(c => c.Make).Distinct().OrderBy(m => m).ToList();
            ViewBag.Models = _allCars.Select(c => c.Model).Distinct().OrderBy(m => m).ToList();
            ViewBag.SelectedMake = make;
            ViewBag.SelectedModel = model;
            ViewBag.SelectedPriceRange = priceRange;

            return View(filteredCars);
        }

        [HttpPost]
        public JsonResult FilterCars([FromBody] CarFilterRequest request)
        {
            var filteredCars = FilterCars(request.Make, request.Model, request.PriceRange);

            return Json(new CarFilterResponse
            {
                Cars = filteredCars,
                TotalCars = filteredCars.Count,
                ResultsText = $"Showing {filteredCars.Count} car(s)"
            });
        }

        private List<Car> FilterCars(string? make, string? model, string? priceRange)
        {
            var query = _allCars.AsEnumerable();

            // Filter by make
            if (!string.IsNullOrEmpty(make) && make != "All")
            {
                query = query.Where(c => c.Make.Equals(make, StringComparison.OrdinalIgnoreCase));
            }

            // Filter by model
            if (!string.IsNullOrEmpty(model) && model != "All")
            {
                query = query.Where(c => c.Model.Equals(model, StringComparison.OrdinalIgnoreCase));
            }

            // Filter by price range
            if (!string.IsNullOrEmpty(priceRange) && priceRange != "All")
            {
                var (minPrice, maxPrice) = ParsePriceRange(priceRange);
                query = query.Where(c => c.Price >= minPrice && c.Price <= maxPrice);
            }

            return query.ToList();
        }

        private (decimal minPrice, decimal maxPrice) ParsePriceRange(string priceRange)
        {
            return priceRange switch
            {
                "0-25000" => (0, 25000),
                "25000-35000" => (25000, 35000),
                "35000-50000" => (35000, 50000),
                "50000-70000" => (50000, 70000),
                "70000+" => (70000, decimal.MaxValue),
                _ => (0, decimal.MaxValue)
            };
        }

        public IActionResult CarsTwo()
        {
            ViewData["Title"] = "Cars Two - AutoVault";
            ViewData["BodyClass"] = "with-sidebar";
            ViewData["HeaderStyle"] = "three";
            return View(_allCars);
        }

        public IActionResult CarsThree()
        {
            ViewData["Title"] = "Cars Three - AutoVault";
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
            return View(car);
        }

        public IActionResult DetailsTwo(int? id)
        {
            ViewData["Title"] = "Car Details Two - AutoVault";
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
