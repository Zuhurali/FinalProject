using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class BlogController : Controller
    {
        private readonly ICarApiService _carApi;

        public BlogController(ICarApiService carApi)
        {
            _carApi = carApi;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Blog - AutoVault";
            return View();
        }

        public async Task<IActionResult> BlogTwo()
        {
            ViewData["Title"] = "Blog Two - AutoVault";
            await LoadSidebarCarsAsync();
            return View();
        }

        public async Task<IActionResult> Details(int? id)
        {
            ViewData["Title"] = "Blog Details - AutoVault";
            ViewData["PostId"] = id is > 0 and <= 8 ? id : 1;
            await LoadSidebarCarsAsync();
            return View();
        }

        private async Task LoadSidebarCarsAsync()
        {
            IReadOnlyList<Car> cars;
            try
            {
                cars = await CarApiMapper.LoadCarsAsync(_carApi, HttpContext.RequestAborted);
            }
            catch (CarApiException exception)
            {
                ViewData["CarApiError"] = exception.Message;
                cars = Array.Empty<Car>();
            }

            ViewBag.Cars = cars;
            ViewBag.BrandCounts = cars
                .GroupBy(car => car.Make)
                .Select(group => new { Make = group.Key, Count = group.Count() })
                .OrderBy(group => group.Make)
                .ToList();
        }
    }
}