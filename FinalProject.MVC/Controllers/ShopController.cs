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
            _allProducts = TemplateProducts();
        }

        private static List<ShopProduct> TemplateProducts()
        {
            return new List<ShopProduct>
            {
                Part(1, "Car Parts Gears and Equipment", 400, "11.webp"),
                Part(2, "Power Steering Pump", 599, "12.webp"),
                Part(3, "High-Performance Tires", 399, "13.webp"),
                Part(4, "Micrometer with check ring", 790, "14.webp"),
                Part(5, "Car Parts Mufflers And Pipes", 270, "15.webp"),
                Part(6, "Car Gears Machine part", 699, "16.webp")
            };
        }

        private static ShopProduct Part(int id, string name, decimal price, string image)
        {
            return new ShopProduct
            {
                Id = id,
                Name = name,
                Price = price,
                Category = "Parts",
                ImagePath = "~/assets/images/portfolio/" + image,
                Rating = 4.5,
                Description = name,
                InStock = true,
                SearchKeywords = name
            };
        }

        private static ShopProduct CopyPart(ShopProduct product)
        {
            return Part(
                product.Id,
                product.Name,
                product.Price,
                product.ImagePath.Replace("~/assets/images/portfolio/", ""));
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

            ViewBag.CarMakes = _allProducts.Select(p => p.CarMake).Where(make => !string.IsNullOrWhiteSpace(make)).Distinct().OrderBy(x => x).ToList();
            ViewBag.CarModels = _allProducts.Select(p => p.CarModel).Where(model => !string.IsNullOrWhiteSpace(model)).Distinct().OrderBy(x => x).ToList();
            
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
            var products = new List<ShopProduct>();
            for (var copy = 0; copy < 3; copy++)
            {
                products.AddRange(_allProducts.Select(CopyPart));
            }

            var response = new ShopFilterResponse
            {
                Products = products,
                TotalProducts = products.Count,
                CurrentPage = 1,
                TotalPages = 1,
                HasMorePages = false,
                ResultsText = $"Showing 1-{products.Count} of {products.Count} results"
            };
            
            return View(response);
        }

        public IActionResult ShopThree()
        {
            ViewData["Title"] = "Shop Three - AutoVault";
            
            // Use the same products from the main shop - show 12 products initially (col-lg-6 grid = 2 per row)
            var order = new[] { 1, 2, 3, 4, 5, 6, 1, 2, 1, 2, 3, 4 };
            var products = order
                .Select(id => CopyPart(_allProducts.First(product => product.Id == id)))
                .ToList();

            var response = new ShopFilterResponse
            {
                Products = products,
                TotalProducts = products.Count,
                CurrentPage = 1,
                TotalPages = 1,
                HasMorePages = false,
                ResultsText = $"Showing 1-{products.Count} of {products.Count} results"
            };
            
            return View(response);
        }

        public IActionResult Details(int? id)
        {
            ViewData["Title"] = "Shop Details - AutoVault";

            if (id == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index");
            }

            var product = _allProducts.FirstOrDefault(p => p.Id == id.Value);

            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index");
            }

            return View(product);
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
