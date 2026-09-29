using Microsoft.AspNetCore.Mvc;
using FinalProject.MVC.Models;

namespace FinalProject.MVC.Controllers
{
    public class CarsController : Controller
    {
        private readonly List<Car> _allCars;

        public CarsController()
        {
            // Initialize car inventory with realistic data
            _allCars = new List<Car>
            {
                // Honda Cars
                new Car { Id = 1, Name = "Honda Civic", Make = "Honda", Model = "Civic", Price = 24500, Mileage = 35000, Fuel = "Petrol", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/04.webp", Badge = "Stocks", Year = 2022, Color = "Silver", MonthlyPayment = 450, Description = "Reliable and fuel-efficient Honda Civic" },
                new Car { Id = 2, Name = "Honda Accord", Make = "Honda", Model = "Accord", Price = 28900, Mileage = 28000, Fuel = "Petrol", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/05.webp", Badge = "New", Year = 2023, Color = "Black", MonthlyPayment = 520, Description = "Spacious and comfortable Honda Accord" },
                new Car { Id = 3, Name = "Honda CR-V", Make = "Honda", Model = "CR-V", Price = 32500, Mileage = 20000, Fuel = "Petrol", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/06.webp", Badge = "Trending", Year = 2023, Color = "White", MonthlyPayment = 580, Description = "Versatile Honda CR-V SUV" },
                new Car { Id = 4, Name = "Honda Pilot", Make = "Honda", Model = "Pilot", Price = 38900, Mileage = 15000, Fuel = "Petrol", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/07.webp", Badge = "New", Year = 2024, Color = "Blue", MonthlyPayment = 680, Description = "Family-friendly Honda Pilot" },

                // Mercedes-Benz Cars
                new Car { Id = 5, Name = "Mercedes-Benz C-Class", Make = "Mercedes-Benz", Model = "C-Class", Price = 45000, Mileage = 12000, Fuel = "Petrol", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/08.webp", Badge = "New", Year = 2023, Color = "Silver", MonthlyPayment = 780, Description = "Luxury Mercedes-Benz C-Class" },
                new Car { Id = 6, Name = "Mercedes-Benz E-Class", Make = "Mercedes-Benz", Model = "E-Class", Price = 55000, Mileage = 8000, Fuel = "Diesel", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/09.webp", Badge = "Trending", Year = 2023, Color = "Black", MonthlyPayment = 920, Description = "Executive Mercedes-Benz E-Class" },
                new Car { Id = 7, Name = "Mercedes-Benz GLE", Make = "Mercedes-Benz", Model = "GLE", Price = 68000, Mileage = 5000, Fuel = "Petrol", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/10.webp", Badge = "New", Year = 2024, Color = "White", MonthlyPayment = 1150, Description = "Premium Mercedes-Benz GLE SUV" },
                new Car { Id = 8, Name = "Mercedes-Benz S-Class", Make = "Mercedes-Benz", Model = "S-Class", Price = 95000, Mileage = 3000, Fuel = "Petrol", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/11.webp", Badge = "New", Year = 2024, Color = "Black", MonthlyPayment = 1580, Description = "Flagship Mercedes-Benz S-Class" },

                // BMW Cars
                new Car { Id = 9, Name = "BMW 3 Series", Make = "BMW", Model = "3 Series", Price = 42000, Mileage = 18000, Fuel = "Petrol", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/12.webp", Badge = "Stocks", Year = 2022, Color = "Blue", MonthlyPayment = 720, Description = "Sporty BMW 3 Series" },
                new Car { Id = 10, Name = "BMW 5 Series", Make = "BMW", Model = "5 Series", Price = 52000, Mileage = 10000, Fuel = "Diesel", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/13.webp", Badge = "Trending", Year = 2023, Color = "Gray", MonthlyPayment = 880, Description = "Performance-oriented BMW 5 Series" },
                new Car { Id = 11, Name = "BMW X3", Make = "BMW", Model = "X3", Price = 48000, Mileage = 14000, Fuel = "Petrol", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/14.webp", Badge = "New", Year = 2023, Color = "White", MonthlyPayment = 820, Description = "Compact BMW X3 SUV" },
                new Car { Id = 12, Name = "BMW X5", Make = "BMW", Model = "X5", Price = 65000, Mileage = 6000, Fuel = "Diesel", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/15.webp", Badge = "New", Year = 2024, Color = "Black", MonthlyPayment = 1100, Description = "Luxury BMW X5 SUV" },

                // Toyota Cars
                new Car { Id = 13, Name = "Toyota Camry", Make = "Toyota", Model = "Camry", Price = 26500, Mileage = 30000, Fuel = "Petrol", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/01.webp", Badge = "Stocks", Year = 2022, Color = "Silver", MonthlyPayment = 480, Description = "Reliable Toyota Camry" },
                new Car { Id = 14, Name = "Toyota Corolla", Make = "Toyota", Model = "Corolla", Price = 22000, Mileage = 40000, Fuel = "Petrol", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/02.webp", Badge = "Stocks", Year = 2021, Color = "White", MonthlyPayment = 400, Description = "Economical Toyota Corolla" },
                new Car { Id = 15, Name = "Toyota RAV4", Make = "Toyota", Model = "RAV4", Price = 31000, Mileage = 22000, Fuel = "Hybrid", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/03.webp", Badge = "Trending", Year = 2023, Color = "Blue", MonthlyPayment = 550, Description = "Hybrid Toyota RAV4" },
                new Car { Id = 16, Name = "Toyota Highlander", Make = "Toyota", Model = "Highlander", Price = 38000, Mileage = 16000, Fuel = "Petrol", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/04.webp", Badge = "New", Year = 2023, Color = "Gray", MonthlyPayment = 650, Description = "Spacious Toyota Highlander" },

                // Ford Cars
                new Car { Id = 17, Name = "Ford Mustang", Make = "Ford", Model = "Mustang", Price = 35000, Mileage = 25000, Fuel = "Petrol", Transmission = "Manual", ImagePath = "~/assets/images/portfolio/05.webp", Badge = "Trending", Year = 2022, Color = "Red", MonthlyPayment = 620, Description = "Iconic Ford Mustang" },
                new Car { Id = 18, Name = "Ford F-150", Make = "Ford", Model = "F-150", Price = 42000, Mileage = 20000, Fuel = "Petrol", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/06.webp", Badge = "Stocks", Year = 2023, Color = "Black", MonthlyPayment = 720, Description = "Powerful Ford F-150 truck" },
                new Car { Id = 19, Name = "Ford Explorer", Make = "Ford", Model = "Explorer", Price = 39500, Mileage = 18000, Fuel = "Petrol", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/07.webp", Badge = "New", Year = 2023, Color = "White", MonthlyPayment = 680, Description = "Adventure-ready Ford Explorer" },

                // Volkswagen Cars
                new Car { Id = 20, Name = "Volkswagen Golf", Make = "Volkswagen", Model = "Golf", Price = 25000, Mileage = 32000, Fuel = "Petrol", Transmission = "Manual", ImagePath = "~/assets/images/portfolio/08.webp", Badge = "Stocks", Year = 2022, Color = "Silver", MonthlyPayment = 450, Description = "Compact Volkswagen Golf" },
                new Car { Id = 21, Name = "Volkswagen Passat", Make = "Volkswagen", Model = "Passat", Price = 28000, Mileage = 26000, Fuel = "Diesel", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/09.webp", Badge = "Stocks", Year = 2022, Color = "Blue", MonthlyPayment = 500, Description = "Comfortable Volkswagen Passat" },
                new Car { Id = 22, Name = "Volkswagen Tiguan", Make = "Volkswagen", Model = "Tiguan", Price = 33000, Mileage = 19000, Fuel = "Petrol", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/10.webp", Badge = "New", Year = 2023, Color = "Black", MonthlyPayment = 580, Description = "Versatile Volkswagen Tiguan" },

                // Mazda Cars
                new Car { Id = 23, Name = "Mazda CX-5", Make = "Mazda", Model = "CX-5", Price = 29500, Mileage = 24000, Fuel = "Petrol", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/11.webp", Badge = "Trending", Year = 2023, Color = "Red", MonthlyPayment = 520, Description = "Stylish Mazda CX-5" },
                new Car { Id = 24, Name = "Mazda3", Make = "Mazda", Model = "Mazda3", Price = 23500, Mileage = 28000, Fuel = "Petrol", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/12.webp", Badge = "Stocks", Year = 2022, Color = "White", MonthlyPayment = 430, Description = "Sporty Mazda3" },

                // Mitsubishi Cars
                new Car { Id = 25, Name = "Mitsubishi Outlander", Make = "Mitsubishi", Model = "Outlander", Price = 30000, Mileage = 21000, Fuel = "Petrol", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/13.webp", Badge = "Stocks", Year = 2023, Color = "Silver", MonthlyPayment = 540, Description = "Practical Mitsubishi Outlander" },
                new Car { Id = 26, Name = "Mitsubishi Eclipse Cross", Make = "Mitsubishi", Model = "Eclipse Cross", Price = 27500, Mileage = 25000, Fuel = "Petrol", Transmission = "Automatic", ImagePath = "~/assets/images/portfolio/14.webp", Badge = "Stocks", Year = 2022, Color = "Black", MonthlyPayment = 490, Description = "Dynamic Mitsubishi Eclipse Cross" }
            };
        }

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
            return View();
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