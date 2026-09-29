using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class CarsController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Cars - AutoVault";
            return View();
        }

        public IActionResult CarsOne()
        {
            ViewData["Title"] = "Cars One - AutoVault";
            return View();
        }

        public IActionResult CarsTwo()
        {
            ViewData["Title"] = "Cars Two - AutoVault";
            return View();
        }

        public IActionResult CarsThree()
        {
            ViewData["Title"] = "Cars Three - AutoVault";
            return View();
        }

        public IActionResult Details(int? id)
        {
            ViewData["Title"] = "Car Details - AutoVault";
            ViewData["CarId"] = id ?? 1;
            return View();
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