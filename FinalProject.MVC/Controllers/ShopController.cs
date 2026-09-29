using Microsoft.AspNetCore.Mvc;
using FinalProject.MVC.Models;

namespace FinalProject.MVC.Controllers
{
    public class ShopController : Controller
    {
        private readonly List<ShopProduct> _allProducts;

        public ShopController()
        {
            // Initialize with original Autovault shop products (first 6 are exact matches)
            _allProducts = new List<ShopProduct>
            {
                // Original Autovault products - EXACT matches from shop.html
                new ShopProduct { Id = 1, Name = "Car Parts Gears and Equipment", Price = 400, Category = "Gears", CarMake = "Mazda", CarModel = "155", ImagePath = "~/assets/images/portfolio/11.webp", Rating = 4.5, Description = "High-quality car gears and equipment" },
                new ShopProduct { Id = 2, Name = "Power Steering Pump", Price = 599, Category = "Steering", CarMake = "Honda", CarModel = "150", ImagePath = "~/assets/images/portfolio/12.webp", Rating = 4.5, Description = "Professional power steering pump" },
                new ShopProduct { Id = 3, Name = "High-Performance Tires", Price = 399, Category = "Tires", CarMake = "Citroen", CarModel = "151", ImagePath = "~/assets/images/portfolio/13.webp", Rating = 4.5, Description = "Premium high-performance tires" },
                new ShopProduct { Id = 4, Name = "Micrometer with check ring", Price = 790, Category = "Tools", CarMake = "Mitsubishi", CarModel = "152", ImagePath = "~/assets/images/portfolio/14.webp", Rating = 4.5, Description = "Precision measurement tool" },
                new ShopProduct { Id = 5, Name = "Car Parts Mufflers And Pipes", Price = 270, Category = "Exhaust", CarMake = "Peugeot", CarModel = "156", ImagePath = "~/assets/images/portfolio/15.webp", Rating = 4.5, Description = "Quality mufflers and exhaust pipes" },
                new ShopProduct { Id = 6, Name = "Car Gears Machine part", Price = 699, Category = "Gears", CarMake = "Mazda", CarModel = "155", ImagePath = "~/assets/images/portfolio/16.webp", Rating = 4.5, Description = "Machine parts and gears" },
                
                // Additional automotive products using other portfolio images
                new ShopProduct { Id = 7, Name = "Brake Disc Premium Set", Price = 320, Category = "Brakes", CarMake = "Honda", CarModel = "150", ImagePath = "~/assets/images/portfolio/17.webp", Rating = 4.8, Description = "Premium brake disc set" },
                new ShopProduct { Id = 8, Name = "Engine Oil Filter Kit", Price = 89, Category = "Filters", CarMake = "Citroen", CarModel = "151", ImagePath = "~/assets/images/portfolio/18.webp", Rating = 4.3, Description = "Complete oil filter kit" },
                new ShopProduct { Id = 9, Name = "Suspension Springs Heavy-Duty", Price = 450, Category = "Suspension", CarMake = "Mitsubishi", CarModel = "152", ImagePath = "~/assets/images/portfolio/19.webp", Rating = 4.6, Description = "Heavy-duty suspension springs" },
                new ShopProduct { Id = 10, Name = "LED Headlight Assembly", Price = 280, Category = "Lighting", CarMake = "Peugeot", CarModel = "156", ImagePath = "~/assets/images/portfolio/01.webp", Rating = 4.4, Description = "Complete LED headlight assembly" },
                new ShopProduct { Id = 11, Name = "Transmission Fluid Premium", Price = 165, Category = "Fluids", CarMake = "Mazda", CarModel = "155", ImagePath = "~/assets/images/portfolio/02.webp", Rating = 4.2, Description = "High-grade transmission fluid" },
                new ShopProduct { Id = 12, Name = "Performance Air Filter", Price = 145, Category = "Filters", CarMake = "Honda", CarModel = "150", ImagePath = "~/assets/images/portfolio/03.webp", Rating = 4.1, Description = "Performance air filter element" },
                new ShopProduct { Id = 13, Name = "Spark Plug Set Premium", Price = 220, Category = "Engine", CarMake = "Citroen", CarModel = "151", ImagePath = "~/assets/images/portfolio/04.webp", Rating = 4.7, Description = "Premium spark plug set" },
                new ShopProduct { Id = 14, Name = "Radiator Assembly Complete", Price = 580, Category = "Cooling", CarMake = "Mitsubishi", CarModel = "152", ImagePath = "~/assets/images/portfolio/05.webp", Rating = 4.5, Description = "Complete radiator assembly" },
                new ShopProduct { Id = 15, Name = "Fuel Pump Module Electric", Price = 720, Category = "Fuel", CarMake = "Peugeot", CarModel = "156", ImagePath = "~/assets/images/portfolio/06.webp", Rating = 4.6, Description = "Electric fuel pump module" },
                new ShopProduct { Id = 16, Name = "Brake Pads Ceramic Set", Price = 195, Category = "Brakes", CarMake = "Mazda", CarModel = "155", ImagePath = "~/assets/images/portfolio/07.webp", Rating = 4.3, Description = "Ceramic brake pads set" },
                new ShopProduct { Id = 17, Name = "Steering Wheel Premium", Price = 340, Category = "Steering", CarMake = "Honda", CarModel = "150", ImagePath = "~/assets/images/portfolio/08.webp", Rating = 4.4, Description = "Premium steering wheel" },
                new ShopProduct { Id = 18, Name = "Tire Pressure Monitoring", Price = 125, Category = "Tires", CarMake = "Citroen", CarModel = "151", ImagePath = "~/assets/images/portfolio/09.webp", Rating = 4.2, Description = "Tire pressure monitoring system" }
            };
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Shop - AutoVault";
            
            // Initial load - show first 6 products (matching original template)
            var initialProducts = _allProducts.Take(6).ToList();
            var response = new ShopFilterResponse
            {
                Products = initialProducts,
                TotalProducts = _allProducts.Count,
                CurrentPage = 1,
                TotalPages = (int)Math.Ceiling(_allProducts.Count / 6.0),
                HasMorePages = _allProducts.Count > 6,
                ResultsText = $"Showing 1-{Math.Min(6, _allProducts.Count)} of {_allProducts.Count} results"
            };

            ViewBag.CarMakes = _allProducts.Select(p => p.CarMake).Distinct().OrderBy(x => x).ToList();
            ViewBag.CarModels = _allProducts.Select(p => p.CarModel).Distinct().OrderBy(x => x).ToList();
            
            return View(response);
        }

        [HttpPost]
        public JsonResult FilterProducts([FromBody] ShopFilterRequest request)
        {
            var filteredProducts = _allProducts.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.CarMake) && request.CarMake != "Car Make")
            {
                filteredProducts = filteredProducts.Where(p => p.CarMake == request.CarMake);
            }

            if (!string.IsNullOrEmpty(request.CarModel) && request.CarModel != "Car Model")
            {
                filteredProducts = filteredProducts.Where(p => p.CarModel == request.CarModel);
            }

            if (!string.IsNullOrEmpty(request.PriceRange) && request.PriceRange != "Price")
            {
                switch (request.PriceRange)
                {
                    case "Under $200":
                        filteredProducts = filteredProducts.Where(p => p.Price < 200);
                        break;
                    case "Under $400":
                        filteredProducts = filteredProducts.Where(p => p.Price < 400);
                        break;
                    case "Under $600":
                        filteredProducts = filteredProducts.Where(p => p.Price < 600);
                        break;
                    case "Under $800":
                        filteredProducts = filteredProducts.Where(p => p.Price < 800);
                        break;
                    case "All Prices":
                        // No filter applied
                        break;
                }
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(request.SortBy) && request.SortBy != "Sort By")
            {
                switch (request.SortBy)
                {
                    case "Price Low to High":
                        filteredProducts = filteredProducts.OrderBy(p => p.Price);
                        break;
                    case "Price High to Low":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.Price);
                        break;
                    case "Name A-Z":
                        filteredProducts = filteredProducts.OrderBy(p => p.Name);
                        break;
                    case "Name Z-A":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.Name);
                        break;
                    case "Rating":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.Rating);
                        break;
                    default:
                        filteredProducts = filteredProducts.OrderBy(p => p.Id);
                        break;
                }
            }

            var totalFilteredProducts = filteredProducts.Count();
            var pagedProducts = filteredProducts
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var totalPages = (int)Math.Ceiling(totalFilteredProducts / (double)request.PageSize);
            var startItem = (request.Page - 1) * request.PageSize + 1;
            var endItem = Math.Min(request.Page * request.PageSize, totalFilteredProducts);

            var response = new ShopFilterResponse
            {
                Products = pagedProducts,
                TotalProducts = totalFilteredProducts,
                CurrentPage = request.Page,
                TotalPages = totalPages,
                HasMorePages = request.Page < totalPages,
                ResultsText = totalFilteredProducts > 0 
                    ? $"Showing {startItem}-{endItem} of {totalFilteredProducts} results"
                    : "No products found"
            };

            return Json(response);
        }

        [HttpPost]
        public JsonResult LoadMore(int page = 1, int pageSize = 6)
        {
            var products = _allProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var totalPages = (int)Math.Ceiling(_allProducts.Count / (double)pageSize);

            var response = new ShopFilterResponse
            {
                Products = products,
                TotalProducts = _allProducts.Count,
                CurrentPage = page,
                TotalPages = totalPages,
                HasMorePages = page < totalPages,
                ResultsText = ""
            };

            return Json(response);
        }

        public IActionResult ShopTwo()
        {
            ViewData["Title"] = "Shop Two - AutoVault";
            return View();
        }

        public IActionResult ShopThree()
        {
            ViewData["Title"] = "Shop Three - AutoVault";
            return View();
        }

        public IActionResult Details(int? id)
        {
            ViewData["Title"] = "Shop Details - AutoVault";
            ViewData["ProductId"] = id ?? 1;
            return View();
        }

        public IActionResult Cart()
        {
            ViewData["Title"] = "Cart - AutoVault";
            return View();
        }

        public IActionResult Checkout()
        {
            ViewData["Title"] = "Checkout - AutoVault";
            return View();
        }
    }
}