using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Mappings;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class DealerController : Controller
    {
        private readonly ICarApiService _carApi;

        public DealerController(ICarApiService carApi)
        {
            _carApi = carApi;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Dealer List - AutoVault";
            return View(DealerCatalog.All);
        }

        public async Task<IActionResult> Details(int id)
        {
            var dealer = DealerCatalog.Find(id);
            if (dealer == null)
            {
                return NotFound();
            }

            var related = new List<Car>();
            try
            {
                var cars = (await CarApiMapper.LoadCarsAsync(_carApi, HttpContext.RequestAborted)).ToList();
                if (cars.Count > 0)
                {
                    var start = ((id - 1) * 2) % cars.Count;
                    related.Add(cars[start]);
                    related.Add(cars[(start + 1) % cars.Count]);
                }
            }
            catch (CarApiException exception)
            {
                ViewData["CarApiError"] = exception.Message;
            }

            ViewData["Title"] = $"{dealer.Name} - AutoVault";
            return View(new DealerDetailsViewModel
            {
                Dealer = dealer,
                RelatedCars = related
            });
        }
    }
}
