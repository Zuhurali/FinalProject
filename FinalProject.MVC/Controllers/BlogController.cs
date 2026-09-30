using FinalProject.MVC.Data;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class BlogController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Blog - AutoVault";
            return View();
        }

        public IActionResult BlogTwo()
        {
            ViewData["Title"] = "Blog Two - AutoVault";
            var cars = CarCatalog.Create();
            ViewBag.Cars = cars;
            ViewBag.BrandCounts = cars
                .GroupBy(car => car.Make)
                .Select(group => new { Make = group.Key, Count = group.Count() })
                .OrderBy(group => group.Make)
                .ToList();
            return View();
        }

        public IActionResult Details(int? id)
        {
            ViewData["Title"] = "Blog Details - AutoVault";
            ViewData["PostId"] = id is > 0 and <= 8 ? id : 1;
            var cars = CarCatalog.Create();
            ViewBag.Cars = cars;
            ViewBag.BrandCounts = cars
                .GroupBy(car => car.Make)
                .Select(group => new { Make = group.Key, Count = group.Count() })
                .OrderBy(group => group.Make)
                .ToList();
            return View();
        }
    }
}