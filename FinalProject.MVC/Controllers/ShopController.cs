using Microsoft.AspNetCore.Mvc;
using FinalProject.MVC.Models;
using FinalProject.MVC.Data;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Controllers
{
    public class ShopController : Controller
    {
        private readonly List<ShopProduct> _allProducts;
        private readonly ApplicationDbContext _context;

        public ShopController(ApplicationDbContext context)
        {
            _context = context;
            // Brand-specific automotive products with realistic relationships
            _allProducts = new List<ShopProduct>
            {
                // Mercedes-Benz Products (5 items)
                new ShopProduct 
                { 
                    Id = 1, 
                    Name = "Mercedes-Benz Premium Brake Disc Set", 
                    Price = 450, 
                    Category = "Brakes", 
                    CarMake = "Mercedes-Benz", 
                    CarModel = "C-Class", 
                    ImagePath = "~/assets/images/portfolio/11.webp", 
                    Rating = 4.8, 
                    Description = "OEM-grade brake disc set for Mercedes-Benz C-Class",
                    SearchKeywords = "brake disc mercedes c-class stopping",
                    Color = "Silver",
                    Fuel = "Gasoline"
                },
                new ShopProduct 
                { 
                    Id = 2, 
                    Name = "Mercedes-Benz High-Flow Air Filter", 
                    Price = 85, 
                    Category = "Filters", 
                    CarMake = "Mercedes-Benz", 
                    CarModel = "E-Class", 
                    ImagePath = "~/assets/images/portfolio/12.webp", 
                    Rating = 4.6, 
                    Description = "Performance air filter for Mercedes-Benz E-Class",
                    SearchKeywords = "air filter mercedes e-class performance",
                    Color = "White",
                    Fuel = "Diesel"
                },
                new ShopProduct 
                { 
                    Id = 3, 
                    Name = "Mercedes-Benz Synthetic Oil Filter", 
                    Price = 120, 
                    Category = "Filters", 
                    CarMake = "Mercedes-Benz", 
                    CarModel = "S-Class", 
                    ImagePath = "~/assets/images/portfolio/13.webp", 
                    Rating = 4.9, 
                    Description = "Premium synthetic oil filter for Mercedes-Benz S-Class",
                    SearchKeywords = "oil filter mercedes s-class synthetic",
                    Color = "Black",
                    Fuel = "Gasoline"
                },
                new ShopProduct 
                { 
                    Id = 4, 
                    Name = "Mercedes-Benz Sport Suspension Kit", 
                    Price = 680, 
                    Category = "Suspension", 
                    CarMake = "Mercedes-Benz", 
                    CarModel = "C-Class", 
                    ImagePath = "~/assets/images/portfolio/14.webp", 
                    Rating = 4.7, 
                    Description = "Performance suspension upgrade for Mercedes-Benz",
                    SearchKeywords = "suspension mercedes c-class sport handling",
                    Color = "Silver",
                    Fuel = "Gasoline"
                },
                new ShopProduct 
                { 
                    Id = 5, 
                    Name = "Mercedes-Benz LED Headlight Assembly", 
                    Price = 520, 
                    Category = "Lighting", 
                    CarMake = "Mercedes-Benz", 
                    CarModel = "E-Class", 
                    ImagePath = "~/assets/images/portfolio/15.webp", 
                    Rating = 4.8, 
                    Description = "LED headlight assembly for Mercedes-Benz E-Class",
                    SearchKeywords = "headlight mercedes e-class led lighting",
                    Color = "Chrome",
                    Fuel = "Diesel"
                },

                // BMW Products (5 items)
                new ShopProduct 
                { 
                    Id = 6, 
                    Name = "BMW Performance Brake Disc", 
                    Price = 480, 
                    Category = "Brakes", 
                    CarMake = "BMW", 
                    CarModel = "3 Series", 
                    ImagePath = "~/assets/images/portfolio/16.webp", 
                    Rating = 4.8, 
                    Description = "High-performance brake disc for BMW 3 Series",
                    SearchKeywords = "brake disc bmw 3-series stopping performance",
                    Color = "Silver",
                    Fuel = "Gasoline"
                },
                new ShopProduct 
                { 
                    Id = 7, 
                    Name = "BMW M Performance Air Filter", 
                    Price = 95, 
                    Category = "Filters", 
                    CarMake = "BMW", 
                    CarModel = "5 Series", 
                    ImagePath = "~/assets/images/portfolio/17.webp", 
                    Rating = 4.7, 
                    Description = "M Performance air filter for BMW 5 Series",
                    SearchKeywords = "air filter bmw 5-series m performance",
                    Color = "Blue",
                    Fuel = "Gasoline"
                },
                new ShopProduct 
                { 
                    Id = 8, 
                    Name = "BMW Premium Oil Filter Kit", 
                    Price = 110, 
                    Category = "Filters", 
                    CarMake = "BMW", 
                    CarModel = "X5", 
                    ImagePath = "~/assets/images/portfolio/18.webp", 
                    Rating = 4.6, 
                    Description = "OEM oil filter kit for BMW X5",
                    SearchKeywords = "oil filter bmw x5 maintenance",
                    Color = "Black",
                    Fuel = "Diesel"
                },
                new ShopProduct 
                { 
                    Id = 9, 
                    Name = "BMW Adaptive Suspension Module", 
                    Price = 720, 
                    Category = "Suspension", 
                    CarMake = "BMW", 
                    CarModel = "5 Series", 
                    ImagePath = "~/assets/images/portfolio/19.webp", 
                    Rating = 4.9, 
                    Description = "Adaptive suspension system for BMW 5 Series",
                    SearchKeywords = "suspension bmw 5-series adaptive comfort",
                    Color = "Silver",
                    Fuel = "Gasoline"
                },
                new ShopProduct 
                { 
                    Id = 10, 
                    Name = "BMW Xenon Headlight Set", 
                    Price = 580, 
                    Category = "Lighting", 
                    CarMake = "BMW", 
                    CarModel = "3 Series", 
                    ImagePath = "~/assets/images/portfolio/01.webp", 
                    Rating = 4.8, 
                    Description = "Xenon headlight upgrade for BMW 3 Series",
                    SearchKeywords = "headlight bmw 3-series xenon lighting",
                    Color = "White",
                    Fuel = "Gasoline"
                },

                // Honda Products (5 items)
                new ShopProduct 
                { 
                    Id = 11, 
                    Name = "Honda Ceramic Brake Pads", 
                    Price = 180, 
                    Category = "Brakes", 
                    CarMake = "Honda", 
                    CarModel = "Civic", 
                    ImagePath = "~/assets/images/portfolio/02.webp", 
                    Rating = 4.5, 
                    Description = "Ceramic brake pads for Honda Civic",
                    SearchKeywords = "brake pads honda civic ceramic stopping",
                    Color = "Gray",
                    Fuel = "Gasoline"
                },
                new ShopProduct 
                { 
                    Id = 12, 
                    Name = "Honda High-Efficiency Air Filter", 
                    Price = 45, 
                    Category = "Filters", 
                    CarMake = "Honda", 
                    CarModel = "Accord", 
                    ImagePath = "~/assets/images/portfolio/03.webp", 
                    Rating = 4.4, 
                    Description = "High-efficiency air filter for Honda Accord",
                    SearchKeywords = "air filter honda accord efficiency",
                    Color = "White",
                    Fuel = "Gasoline"
                },
                new ShopProduct 
                { 
                    Id = 13, 
                    Name = "Honda Genuine Oil Filter", 
                    Price = 35, 
                    Category = "Filters", 
                    CarMake = "Honda", 
                    CarModel = "CR-V", 
                    ImagePath = "~/assets/images/portfolio/04.webp", 
                    Rating = 4.6, 
                    Description = "Genuine Honda oil filter for CR-V",
                    SearchKeywords = "oil filter honda cr-v genuine maintenance",
                    Color = "Black",
                    Fuel = "Gasoline"
                },
                new ShopProduct 
                { 
                    Id = 14, 
                    Name = "Honda Sport Suspension Kit", 
                    Price = 320, 
                    Category = "Suspension", 
                    CarMake = "Honda", 
                    CarModel = "Civic", 
                    ImagePath = "~/assets/images/portfolio/05.webp", 
                    Rating = 4.5, 
                    Description = "Sport suspension lowering kit for Honda Civic",
                    SearchKeywords = "suspension honda civic sport lowering",
                    Color = "Red",
                    Fuel = "Gasoline"
                },
                new ShopProduct 
                { 
                    Id = 15, 
                    Name = "Honda LED Headlight Conversion", 
                    Price = 280, 
                    Category = "Lighting", 
                    CarMake = "Honda", 
                    CarModel = "Accord", 
                    ImagePath = "~/assets/images/portfolio/06.webp", 
                    Rating = 4.7, 
                    Description = "LED headlight conversion kit for Honda Accord",
                    SearchKeywords = "headlight honda accord led conversion",
                    Color = "White",
                    Fuel = "Gasoline"
                },

                // Volkswagen Products (5 items)
                new ShopProduct 
                { 
                    Id = 16, 
                    Name = "Volkswagen Brake System Complete", 
                    Price = 380, 
                    Category = "Brakes", 
                    CarMake = "Volkswagen", 
                    CarModel = "Golf", 
                    ImagePath = "~/assets/images/portfolio/07.webp", 
                    Rating = 4.6, 
                    Description = "Complete brake system for Volkswagen Golf",
                    SearchKeywords = "brake system volkswagen golf complete",
                    Color = "Silver",
                    Fuel = "Gasoline"
                },
                new ShopProduct 
                { 
                    Id = 17, 
                    Name = "Volkswagen Performance Air Filter", 
                    Price = 65, 
                    Category = "Filters", 
                    CarMake = "Volkswagen", 
                    CarModel = "Passat", 
                    ImagePath = "~/assets/images/portfolio/08.webp", 
                    Rating = 4.5, 
                    Description = "Performance air filter for Volkswagen Passat",
                    SearchKeywords = "air filter volkswagen passat performance",
                    Color = "Blue",
                    Fuel = "Diesel"
                },
                new ShopProduct 
                { 
                    Id = 18, 
                    Name = "Volkswagen Premium Oil Filter", 
                    Price = 55, 
                    Category = "Filters", 
                    CarMake = "Volkswagen", 
                    CarModel = "Tiguan", 
                    ImagePath = "~/assets/images/portfolio/09.webp", 
                    Rating = 4.4, 
                    Description = "Premium oil filter for Volkswagen Tiguan",
                    SearchKeywords = "oil filter volkswagen tiguan premium",
                    Color = "Black",
                    Fuel = "Gasoline"
                },
                new ShopProduct 
                { 
                    Id = 19, 
                    Name = "Volkswagen Comfort Suspension", 
                    Price = 450, 
                    Category = "Suspension", 
                    CarMake = "Volkswagen", 
                    CarModel = "Golf", 
                    ImagePath = "~/assets/images/portfolio/11.webp", 
                    Rating = 4.6, 
                    Description = "Comfort suspension upgrade for Volkswagen Golf",
                    SearchKeywords = "suspension volkswagen golf comfort upgrade",
                    Color = "Gray",
                    Fuel = "Gasoline"
                },
                new ShopProduct 
                { 
                    Id = 20, 
                    Name = "Volkswagen Bi-Xenon Headlights", 
                    Price = 340, 
                    Category = "Lighting", 
                    CarMake = "Volkswagen", 
                    CarModel = "Passat", 
                    ImagePath = "~/assets/images/portfolio/12.webp", 
                    Rating = 4.7, 
                    Description = "Bi-Xenon headlights for Volkswagen Passat",
                    SearchKeywords = "headlight volkswagen passat xenon bi-xenon",
                    Color = "Chrome",
                    Fuel = "Diesel"
                },

                // Mitsubishi Products (4 items)
                new ShopProduct 
                { 
                    Id = 21, 
                    Name = "Mitsubishi Heavy-Duty Brake Disc", 
                    Price = 220, 
                    Category = "Brakes", 
                    CarMake = "Mitsubishi", 
                    CarModel = "Outlander", 
                    ImagePath = "~/assets/images/portfolio/13.webp", 
                    Rating = 4.5, 
                    Description = "Heavy-duty brake disc for Mitsubishi Outlander",
                    SearchKeywords = "brake disc mitsubishi outlander heavy-duty",
                    Color = "Silver",
                    Fuel = "Gasoline"
                },
                new ShopProduct 
                { 
                    Id = 22, 
                    Name = "Mitsubishi Long-Life Air Filter", 
                    Price = 55, 
                    Category = "Filters", 
                    CarMake = "Mitsubishi", 
                    CarModel = "Lancer", 
                    ImagePath = "~/assets/images/portfolio/14.webp", 
                    Rating = 4.4, 
                    Description = "Long-life air filter for Mitsubishi Lancer",
                    SearchKeywords = "air filter mitsubishi lancer long-life",
                    Color = "White",
                    Fuel = "Gasoline"
                },
                new ShopProduct 
                { 
                    Id = 23, 
                    Name = "Mitsubishi Genuine Oil Filter", 
                    Price = 42, 
                    Category = "Filters", 
                    CarMake = "Mitsubishi", 
                    CarModel = "Pajero", 
                    ImagePath = "~/assets/images/portfolio/15.webp", 
                    Rating = 4.6, 
                    Description = "Genuine oil filter for Mitsubishi Pajero",
                    SearchKeywords = "oil filter mitsubishi pajero genuine",
                    Color = "Black",
                    Fuel = "Diesel"
                },
                new ShopProduct 
                { 
                    Id = 24, 
                    Name = "Mitsubishi Off-Road Suspension", 
                    Price = 380, 
                    Category = "Suspension", 
                    CarMake = "Mitsubishi", 
                    CarModel = "Outlander", 
                    ImagePath = "~/assets/images/portfolio/16.webp", 
                    Rating = 4.7, 
                    Description = "Off-road suspension kit for Mitsubishi Outlander",
                    SearchKeywords = "suspension mitsubishi outlander off-road",
                    Color = "Black",
                    Fuel = "Gasoline"
                },

                // Lamborghini Products (3 items)
                new ShopProduct 
                { 
                    Id = 25, 
                    Name = "Lamborghini Carbon Ceramic Brakes", 
                    Price = 2500, 
                    Category = "Brakes", 
                    CarMake = "Lamborghini", 
                    CarModel = "Huracán", 
                    ImagePath = "~/assets/images/portfolio/17.webp", 
                    Rating = 5.0, 
                    Description = "Carbon ceramic brake system for Lamborghini Huracán",
                    SearchKeywords = "brake carbon ceramic lamborghini huracan performance",
                    Color = "Black",
                    Fuel = "Gasoline"
                },
                new ShopProduct 
                { 
                    Id = 26, 
                    Name = "Lamborghini Racing Air Filter", 
                    Price = 350, 
                    Category = "Filters", 
                    CarMake = "Lamborghini", 
                    CarModel = "Aventador", 
                    ImagePath = "~/assets/images/portfolio/18.webp", 
                    Rating = 5.0, 
                    Description = "Racing-grade air filter for Lamborghini Aventador",
                    SearchKeywords = "air filter lamborghini aventador racing performance",
                    Color = "Yellow",
                    Fuel = "Gasoline"
                },
                new ShopProduct 
                { 
                    Id = 27, 
                    Name = "Lamborghini Premium Oil Filter", 
                    Price = 280, 
                    Category = "Filters", 
                    CarMake = "Lamborghini", 
                    CarModel = "Huracán", 
                    ImagePath = "~/assets/images/portfolio/19.webp", 
                    Rating = 5.0, 
                    Description = "Premium oil filter for Lamborghini Huracán",
                    SearchKeywords = "oil filter lamborghini huracan premium",
                    Color = "Gold",
                    Fuel = "Gasoline"
                }
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

            // Apply search text filter (name, description, keywords)
            if (!string.IsNullOrEmpty(request.SearchText))
            {
                var searchLower = request.SearchText.ToLower();
                filteredProducts = filteredProducts.Where(p => 
                    p.Name.ToLower().Contains(searchLower) || 
                    p.Description.ToLower().Contains(searchLower) ||
                    (p.SearchKeywords != null && p.SearchKeywords.ToLower().Contains(searchLower))
                );
            }

            // Apply category filter
            if (!string.IsNullOrEmpty(request.Category))
            {
                filteredProducts = filteredProducts.Where(p => p.Category == request.Category);
            }

            // Apply car make filter (single select - backwards compatibility)
            if (!string.IsNullOrEmpty(request.CarMake) && request.CarMake != "Car Make")
            {
                filteredProducts = filteredProducts.Where(p => p.CarMake == request.CarMake);
            }

            // Apply selected makes filter (multi-select from ShopTwo)
            if (request.SelectedMakes != null && request.SelectedMakes.Any())
            {
                filteredProducts = filteredProducts.Where(p => request.SelectedMakes.Contains(p.CarMake));
            }

            // Apply car model filter (single select - backwards compatibility)
            if (!string.IsNullOrEmpty(request.CarModel) && request.CarModel != "Car Model")
            {
                filteredProducts = filteredProducts.Where(p => p.CarModel == request.CarModel);
            }

            // Apply selected models filter (multi-select from ShopTwo)
            if (request.SelectedModels != null && request.SelectedModels.Any())
            {
                filteredProducts = filteredProducts.Where(p => request.SelectedModels.Contains(p.CarModel));
            }

            // Apply price range filter (dropdown - backwards compatibility)
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

            // Apply min/max price filter (slider from ShopTwo)
            if (request.MinPrice.HasValue)
            {
                filteredProducts = filteredProducts.Where(p => p.Price >= request.MinPrice.Value);
            }

            if (request.MaxPrice.HasValue)
            {
                filteredProducts = filteredProducts.Where(p => p.Price <= request.MaxPrice.Value);
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
            
            // Use the same products from the main shop
            var products = _allProducts.Take(12).ToList(); // Show 12 products initially
            
            var response = new ShopFilterResponse
            {
                Products = products,
                TotalProducts = _allProducts.Count,
                CurrentPage = 1,
                TotalPages = (int)Math.Ceiling(_allProducts.Count / 12.0),
                HasMorePages = _allProducts.Count > 12,
                ResultsText = $"Showing 1-{Math.Min(12, _allProducts.Count)} of {_allProducts.Count} results"
            };
            
            return View(response);
        }

        public IActionResult ShopThree()
        {
            ViewData["Title"] = "Shop Three - AutoVault";
            
            // Use the same products from the main shop - show 12 products initially (col-lg-6 grid = 2 per row)
            var products = _allProducts.Take(12).ToList();
            
            var response = new ShopFilterResponse
            {
                Products = products,
                TotalProducts = _allProducts.Count,
                CurrentPage = 1,
                TotalPages = (int)Math.Ceiling(_allProducts.Count / 12.0),
                HasMorePages = _allProducts.Count > 12,
                ResultsText = $"Showing 1-{Math.Min(12, _allProducts.Count)} of {_allProducts.Count} results"
            };
            
            return View(response);
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
            
            // Get cart from session
            var cart = GetCartFromSession();
            
            return View(cart);
        }

        [HttpPost]
        public JsonResult AddToCart([FromBody] AddToCartRequest request)
        {
            var product = _allProducts.FirstOrDefault(p => p.Id == request.ProductId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            var cart = GetCartFromSession();
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = request.Quantity,
                    ImagePath = product.ImagePath
                });
            }

            SaveCartToSession(cart);

            return Json(new
            {
                success = true,
                message = "Product added to cart",
                cartCount = cart.Items.Sum(i => i.Quantity),
                cartTotal = cart.Total
            });
        }

        [HttpPost]
        public JsonResult UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            if (request.Quantity < 1)
            {
                return Json(new { success = false, message = "Quantity must be at least 1" });
            }

            var cart = GetCartFromSession();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

            if (item == null)
            {
                return Json(new { success = false, message = "Item not found in cart" });
            }

            item.Quantity = request.Quantity;
            SaveCartToSession(cart);

            return Json(new
            {
                success = true,
                itemSubtotal = item.Subtotal,
                cartSubtotal = cart.Subtotal,
                cartTotal = cart.Total
            });
        }

        [HttpPost]
        public JsonResult RemoveItem([FromBody] int productId)
        {
            var cart = GetCartFromSession();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (item != null)
            {
                cart.Items.Remove(item);
                SaveCartToSession(cart);
            }

            return Json(new
            {
                success = true,
                cartSubtotal = cart.Subtotal,
                cartTotal = cart.Total,
                isEmpty = cart.IsEmpty
            });
        }

        [HttpPost]
        public async Task<JsonResult> ApplyCoupon([FromBody] ApplyCouponRequest request)
        {
            var cart = GetCartFromSession();

            // Validate coupon code
            if (string.IsNullOrWhiteSpace(request.CouponCode) || request.CouponCode.ToUpper() != "CARS100")
            {
                return Json(new CouponResult
                {
                    Success = false,
                    Message = "Invalid coupon code",
                    DiscountAmount = 0,
                    NewTotal = cart.Total
                });
            }

            // Check if user is authenticated
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Json(new CouponResult
                {
                    Success = false,
                    Message = "Please sign in to use this coupon.",
                    DiscountAmount = 0,
                    NewTotal = cart.Total
                });
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new CouponResult
                {
                    Success = false,
                    Message = "User authentication error. Please sign in again.",
                    DiscountAmount = 0,
                    NewTotal = cart.Total
                });
            }

            // Check if user has already used this coupon
            var existingUsage = await _context.CouponUsages
                .FirstOrDefaultAsync(cu => cu.UserId == userId && cu.CouponCode == "CARS100");

            if (existingUsage != null)
            {
                return Json(new CouponResult
                {
                    Success = false,
                    Message = "You have already used this coupon!",
                    DiscountAmount = 0,
                    NewTotal = cart.Total
                });
            }

            // Apply 10% discount
            var discountAmount = cart.Subtotal * 0.10m;
            cart.DiscountAmount = discountAmount;
            cart.CouponCode = "CARS100";
            SaveCartToSession(cart);

            // Record coupon usage in database
            try
            {
                var couponUsage = new CouponUsage
                {
                    UserId = userId,
                    CouponCode = "CARS100",
                    UsedAt = DateTime.UtcNow,
                    DiscountAmount = discountAmount
                };

                _context.CouponUsages.Add(couponUsage);
                await _context.SaveChangesAsync();

                return Json(new CouponResult
                {
                    Success = true,
                    Message = "Coupon CARS100 applied! 10% discount has been added.",
                    DiscountAmount = cart.DiscountAmount,
                    NewTotal = cart.Total
                });
            }
            catch (DbUpdateException ex)
            {
                // Handle potential unique constraint violation (race condition)
                if (ex.InnerException?.Message.Contains("IX_CouponUsage_UserId_CouponCode") ?? false)
                {
                    // Remove discount from session since it wasn't saved
                    cart.DiscountAmount = 0;
                    cart.CouponCode = null;
                    SaveCartToSession(cart);

                    return Json(new CouponResult
                    {
                        Success = false,
                        Message = "You have already used this coupon!",
                        DiscountAmount = 0,
                        NewTotal = cart.Total
                    });
                }

                // Remove discount from session on any other error
                cart.DiscountAmount = 0;
                cart.CouponCode = null;
                SaveCartToSession(cart);

                return Json(new CouponResult
                {
                    Success = false,
                    Message = "An error occurred while applying the coupon. Please try again.",
                    DiscountAmount = 0,
                    NewTotal = cart.Total
                });
            }
        }

        // Helper methods for session management
        private CartViewModel GetCartFromSession()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson))
            {
                return new CartViewModel();
            }

            return System.Text.Json.JsonSerializer.Deserialize<CartViewModel>(cartJson) ?? new CartViewModel();
        }

        private void SaveCartToSession(CartViewModel cart)
        {
            var cartJson = System.Text.Json.JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString("Cart", cartJson);
        }

        public IActionResult Checkout()
        {
            ViewData["Title"] = "Checkout - AutoVault";
            return View();
        }
    }
}