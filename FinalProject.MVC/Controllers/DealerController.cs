using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class DealerController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Dealer List - AutoVault";
            return View(DealerCatalog.All);
        }

        public IActionResult Details(int id)
        {
            var dealer = DealerCatalog.Find(id);
            if (dealer == null)
            {
                return NotFound();
            }

            var cars = CarCatalog.Create();
            var start = ((id - 1) * 2) % cars.Count;
            var related = new List<Car>
            {
                cars[start],
                cars[(start + 1) % cars.Count]
            };

            ViewData["Title"] = $"{dealer.Name} - AutoVault";
            return View(new DealerDetailsViewModel
            {
                Dealer = dealer,
                RelatedCars = related
            });
        }
    }
}
