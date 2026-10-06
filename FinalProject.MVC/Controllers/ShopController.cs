using Microsoft.AspNetCore.Mvc;
using FinalProject.MVC.Models;
using FinalProject.MVC.Data;
using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Controllers
{
    public class ShopController : Controller
    {
        private readonly List<ShopProduct> _allProducts;
        private readonly ApplicationDbContext _context;
        private readonly IProductRatingApiService _ratingApiService;
        private readonly IApiAccessTokenService _tokenService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShopController(
            ApplicationDbContext context,
            IProductRatingApiService ratingApiService,
            IApiAccessTokenService tokenService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _ratingApiService = ratingApiService;
            _tokenService = tokenService;
            _userManager = userManager;
            // Brand-specific automotive products with realistic relationships
            _allProducts = TemplateProducts();
        }

        internal static List<ShopProduct> TemplateProducts()
        {
            return new List<ShopProduct>
            {
                Part(1, "Car Parts Gears and Equipment", 400, "11.webp", "Parts", 4.5, "Porsche", "911", "Gear, shaft, and workshop equipment set."),
                Part(2, "Brake and Suspension Parts Kit", 599, "12.webp", "Brakes", 4.0, "Mercedes-Benz", "AMG GT", "Brake caliper, drilled rotor, coilover, filters, timing belt, and bearings."),
                Part(3, "High-Performance Tires", 399, "13.webp", "Tires", 5.0, "Toyota", "Highlander", "High-performance tire."),
                Part(4, "Micrometer with check ring", 790, "14.webp", "Tools", 4.5, "", "", "Outside micrometer, bore gauges, and check ring."),
                Part(5, "Car Parts Mufflers And Pipes", 270, "15.webp", "Parts", 4.0, "Nissan", "350Z", "Exhaust mufflers, pipes, and turbo assembly."),
                Part(6, "Car Gears Machine part", 699, "16.webp", "Parts", 4.5, "Lamborghini", "Diablo", "Machine gear and bearing."),
                Part(7, "Engine Assembly", 2490, "parts/engine-assembly.webp", "Parts", 5.0, "Ferrari", "California", "Complete engine assembly."),
                Part(8, "Brake Caliper and Rotor", 549, "parts/brake-caliper.webp", "Brakes", 4.5, "Porsche", "911", "Red brake caliper on a drilled rotor."),
                Part(9, "Coilover Shock Absorber", 429, "parts/coilover-shock.webp", "Suspension", 4.0, "Nissan", "350Z", "Coilover shock absorber with red spring."),
                Part(10, "Pleated Engine Air Filter", 49, "parts/air-filter.webp", "Filters", 4.5, "Toyota", "Highlander", "Pleated engine air filter."),
                Part(11, "Spin-On Oil Filter", 28, "parts/oil-filter.webp", "Filters", 4.0, "Tesla", "Model 3", "Spin-on oil filter."),
                Part(12, "Timing Belt", 89, "parts/timing-belt.webp", "Parts", 4.5, "Maserati", "GranTurismo", "Engine timing belt."),
                Part(13, "Ball Bearing", 64, "parts/ball-bearing.webp", "Parts", 3.5, "Mercedes-Benz", "SL-Class", "Ball bearing."),
                Part(14, "Bore Gauge Set", 340, "parts/bore-gauge.webp", "Tools", 5.0, "", "", "Bore gauge set for engine measurement."),
                Part(15, "Precision Check Ring", 155, "parts/check-ring.webp", "Tools", 4.5, "", "", "Precision check ring for gauge setup."),
                Part(16, "Turbocharger Assembly", 1290, "parts/turbocharger.webp", "Parts", 4.5, "Nissan", "350Z", "Turbocharger and exhaust housing."),
                Part(17, "Exhaust Muffler", 320, "parts/exhaust-muffler.webp", "Parts", 4.0, "Porsche", "911", "Exhaust muffler."),
                Part(18, "Exhaust Tip", 180, "parts/exhaust-tip.webp", "Parts", 4.5, "Mercedes-Benz", "SL-Class", "Exhaust tip."),
                Part(19, "Splined Driveshaft", 260, "parts/splined-shaft.webp", "Parts", 4.0, "Toyota", "Highlander", "Splined driveshaft."),
                Part(20, "Gear Shaft and Fastener Kit", 175, "parts/gear-shaft.webp", "Parts", 4.5, "Rolls-Royce", "Phantom", "Gear shafts, nuts, and fasteners."),
                Part(21, "Outside Micrometer", 220, "parts/outside-micrometer.webp", "Tools", 5.0, "", "", "Outside micrometer.")
            };
        }

        private static ShopProduct Part(
            int id,
            string name,
            decimal price,
            string image,
            string category,
            double rating,
            string make,
            string model,
            string description)
        {
            return new ShopProduct
            {
                Id = id,
                Name = name,
                Price = price,
                Category = category,
                CarMake = make,
                CarModel = model,
                ImagePath = "~/assets/images/portfolio/" + image,
                Rating = 0,
                RatingCount = 0,
                Description = description,
                InStock = true,
                SearchKeywords = string.Join(" ", new[] { name, category, make, model }.Where(value => !string.IsNullOrWhiteSpace(value)))
            };
        }

        private static ShopProduct CopyPart(ShopProduct product)
        {
            return new ShopProduct
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Category = product.Category,
                CarMake = product.CarMake,
                CarModel = product.CarModel,
                ImagePath = product.ImagePath,
                Rating = product.Rating,
                RatingCount = product.RatingCount,
                Description = product.Description,
                InStock = product.InStock,
                Color = product.Color,
                Fuel = product.Fuel,
                SearchKeywords = product.SearchKeywords
            };
        }

        private void SetShopFilters()
        {
            ViewBag.CarMakes = _allProducts
                .Select(p => p.CarMake)
                .Where(make => !string.IsNullOrWhiteSpace(make))
                .Distinct()
                .OrderBy(make => make)
                .ToList();
            ViewBag.CarModels = _allProducts
                .Select(p => p.CarModel)
                .Where(model => !string.IsNullOrWhiteSpace(model))
                .Distinct()
                .OrderBy(model => model)
                .ToList();
            ViewBag.MakeCounts = _allProducts
                .Where(p => !string.IsNullOrWhiteSpace(p.CarMake))
                .GroupBy(p => p.CarMake)
                .OrderBy(group => group.Key)
                .ToDictionary(group => group.Key, group => group.Count());
            ViewBag.ModelCounts = _allProducts
                .Where(p => !string.IsNullOrWhiteSpace(p.CarModel))
                .GroupBy(p => p.CarModel)
                .OrderBy(group => group.Key)
                .ToDictionary(group => group.Key, group => group.Count());
        }

        private async Task ApplyRatingsAsync(
            IEnumerable<ShopProduct> products,
            CancellationToken cancellationToken)
        {
            var productList = products.ToList();
            if (productList.Count == 0)
            {
                return;
            }

            try
            {
                var summaries = await _ratingApiService.GetSummariesAsync(
                    productList.Select(product => product.Id),
                    cancellationToken);
                var byProduct = summaries.ToDictionary(summary => summary.ProductId);

                foreach (var product in productList)
                {
                    if (byProduct.TryGetValue(product.Id, out var summary))
                    {
                        product.Rating = summary.AverageRating;
                        product.RatingCount = summary.TotalCount;
                    }
                }
            }
            catch (CarApiException)
            {
                ViewData["RatingServiceUnavailable"] = true;
            }
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Shop - AutoVault";
            
            // Initial load - show first 6 products (matching original template)
            var initialProducts = _allProducts.Take(6).ToList();
            await ApplyRatingsAsync(initialProducts, cancellationToken);
            var response = new ShopFilterResponse
            {
                Products = initialProducts,
                TotalProducts = _allProducts.Count,
                CurrentPage = 1,
                TotalPages = (int)Math.Ceiling(_allProducts.Count / 6.0),
                HasMorePages = _allProducts.Count > 6,
                ResultsText = $"Showing 1-{Math.Min(6, _allProducts.Count)} of {_allProducts.Count} results"
            };

            SetShopFilters();

            return View(response);
        }

        [HttpPost]
        public async Task<JsonResult> FilterProducts(
            [FromBody] ShopFilterRequest request,
            CancellationToken cancellationToken)
        {
            await ApplyRatingsAsync(_allProducts, cancellationToken);
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
        public async Task<JsonResult> LoadMore(
            int page = 1,
            int pageSize = 6,
            CancellationToken cancellationToken = default)
        {
            var products = _allProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            await ApplyRatingsAsync(products, cancellationToken);

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

        public async Task<IActionResult> ShopTwo(CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Shop Two - AutoVault";
            
            var products = _allProducts.Select(CopyPart).ToList();
            await ApplyRatingsAsync(products, cancellationToken);

            var response = new ShopFilterResponse
            {
                Products = products,
                TotalProducts = products.Count,
                CurrentPage = 1,
                TotalPages = 1,
                HasMorePages = false,
                ResultsText = $"Showing 1-{products.Count} of {products.Count} results"
            };

            SetShopFilters();
            
            return View(response);
        }

        public async Task<IActionResult> ShopThree(CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Shop Three - AutoVault";
            
            // Use the same products from the main shop - show 12 products initially (col-lg-6 grid = 2 per row)
            var order = new[] { 1, 8, 3, 9, 5, 7, 10, 11, 4, 16, 6, 2 };
            var products = order
                .Select(id => CopyPart(_allProducts.First(product => product.Id == id)))
                .ToList();
            await ApplyRatingsAsync(products, cancellationToken);

            var response = new ShopFilterResponse
            {
                Products = products,
                TotalProducts = products.Count,
                CurrentPage = 1,
                TotalPages = 1,
                HasMorePages = false,
                ResultsText = $"Showing 1-{products.Count} of {products.Count} results"
            };

            SetShopFilters();
            
            return View(response);
        }

        public async Task<IActionResult> Details(int? id, CancellationToken cancellationToken)
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

            try
            {
                var summary = await _ratingApiService.GetRatingsAsync(product.Id, cancellationToken);
                product.Rating = summary?.AverageRating ?? 0;
                product.RatingCount = summary?.TotalCount ?? 0;

                if (User.Identity?.IsAuthenticated == true)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        var accessToken = _tokenService.CreateToken(User, roles);
                        var userRating = await _ratingApiService.GetMyRatingAsync(
                            product.Id,
                            accessToken,
                            cancellationToken);
                        ViewBag.UserRating = userRating?.Rating ?? 0;
                    }
                }
            }
            catch (CarApiException)
            {
                ViewData["RatingServiceUnavailable"] = true;
            }

            return View(product);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SubmitRating(
            int productId,
            int rating,
            CancellationToken cancellationToken)
        {
            if (rating is < 1 or > 5)
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return Json(new { success = false, message = "Choose a rating from 1 to 5 stars." });
            }

            if (_allProducts.All(product => product.Id != productId))
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return Json(new { success = false, message = "Product not found." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Json(new { success = false, message = "Please sign in to rate this product." });
            }

            try
            {
                var roles = await _userManager.GetRolesAsync(user);
                var accessToken = _tokenService.CreateToken(User, roles);
                await _ratingApiService.SaveRatingAsync(
                    productId,
                    rating,
                    accessToken,
                    cancellationToken);
                var summary = await _ratingApiService.GetRatingsAsync(productId, cancellationToken);

                return Json(new
                {
                    success = true,
                    message = "Your rating has been saved.",
                    averageRating = summary?.AverageRating ?? rating,
                    totalCount = summary?.TotalCount ?? 1,
                    userRating = rating
                });
            }
            catch (CarApiException)
            {
                Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return Json(new
                {
                    success = false,
                    message = "Ratings are temporarily unavailable. Please try again."
                });
            }
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
