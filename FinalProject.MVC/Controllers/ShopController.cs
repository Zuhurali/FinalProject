using Microsoft.AspNetCore.Mvc;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models;
using FinalProject.MVC.Data;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Controllers
{
    public class ShopController : Controller
    {
        private List<ShopProduct> _allProducts = new();
        private bool _catalogLoaded;
        private readonly ApplicationDbContext _context;
        private readonly IProductRatingApiService _ratingApiService;
        private readonly IApiAccessTokenService _tokenService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IShopCatalogService _shopCatalog;
        private readonly IProductReviewApiService _reviewApiService;
        private readonly IProductPopularityService _popularity;
        private readonly IMarketplaceApiService _marketplaceApi;
        private readonly ICouponService _coupons;

        public ShopController(
            ApplicationDbContext context,
            IProductRatingApiService ratingApiService,
            IApiAccessTokenService tokenService,
            UserManager<ApplicationUser> userManager,
            IShopCatalogService shopCatalog,
            IProductReviewApiService reviewApiService,
            IProductPopularityService popularity,
            IMarketplaceApiService marketplaceApi,
            ICouponService coupons)
        {
            _context = context;
            _ratingApiService = ratingApiService;
            _tokenService = tokenService;
            _userManager = userManager;
            _shopCatalog = shopCatalog;
            _reviewApiService = reviewApiService;
            _popularity = popularity;
            _marketplaceApi = marketplaceApi;
            _coupons = coupons;
        }

        private static ShopProduct CopyPart(ShopProduct product)
        {
            return new ShopProduct
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                IsFavorite = product.IsFavorite,
                Category = product.Category,
                CarMake = product.CarMake,
                CarModel = product.CarModel,
                ImagePath = product.ImagePath,
                Rating = product.Rating,
                RatingCount = product.RatingCount,
                ReviewCount = product.ReviewCount,
                RealSalesCount = product.RealSalesCount,
                DemoSalesCount = product.DemoSalesCount,
                CreatedAt = product.CreatedAt,
                PopularityScore = product.PopularityScore,
                Description = product.Description,
                InStock = product.InStock,
                Quantity = product.Quantity,
                StockStatus = product.StockStatus,
                Brand = product.Brand,
                PartNumber = product.PartNumber,
                Compatibility = product.Compatibility,
                HasOffer = product.HasOffer,
                SalePrice = product.SalePrice,
                DiscountPercent = product.DiscountPercent,
                OfferEndsAt = product.OfferEndsAt,
                OfferEndLabel = product.OfferEndLabel,
                OfferName = product.OfferName,
                Color = product.Color,
                Fuel = product.Fuel,
                SearchKeywords = product.SearchKeywords
            };
        }

        private async Task EnsureCatalogAsync(CancellationToken cancellationToken)
        {
            if (_catalogLoaded)
            {
                return;
            }

            _allProducts = (await _shopCatalog.GetAllAsync(cancellationToken)).ToList();
            await _popularity.ApplyAsync(_allProducts, cancellationToken);
            _catalogLoaded = true;
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
            ViewBag.Categories = _allProducts
                .Select(product => product.Category)
                .Where(category => !string.IsNullOrWhiteSpace(category))
                .Distinct()
                .OrderBy(category => category)
                .ToList();
            ViewBag.Brands = _allProducts
                .Select(product => product.Brand)
                .Where(brand => !string.IsNullOrWhiteSpace(brand))
                .Distinct()
                .OrderBy(brand => brand)
                .ToList();
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

        public const int CatalogPageSize = 8;

        private List<ShopProduct> MatchCatalog(
            string? search,
            string? category,
            string? brand = null,
            int? discountPercent = null,
            int? campaignId = null)
        {
            IEnumerable<ShopProduct> source = _allProducts;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                source = source.Where(product =>
                    product.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    product.Category.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    product.Brand.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    product.PartNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    product.CarMake.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    product.CarModel.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    product.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    product.SearchKeywords.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                source = source.Where(product => string.Equals(product.Category, category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(brand))
            {
                source = source.Where(product => string.Equals(product.Brand, brand, StringComparison.OrdinalIgnoreCase));
            }

            if (campaignId is int liveCampaignId and > 0)
            {
                source = source.Where(product =>
                    product.HasOffer && product.OfferCampaignId == liveCampaignId);
            }
            else if (discountPercent is int percent and > 0)
            {
                source = source.Where(product => product.HasOffer && product.DiscountPercent == percent);
            }

            return source.ToList();
        }

        private static string CatalogResultsText(
            int matchedCount,
            int pageSize,
            int? discountPercent,
            int? campaignId = null)
        {
            if (matchedCount == 0)
            {
                if (campaignId is > 0)
                {
                    return "No active parts currently use this campaign offer.";
                }

                return discountPercent is int percent and > 0
                    ? $"No active parts currently have a {percent}% discount."
                    : "No matching parts";
            }

            return $"Showing 1-{Math.Min(pageSize, matchedCount)} of {matchedCount} results";
        }

        private static List<ShopProduct> OrderCatalog(List<ShopProduct> products, string? sortBy)
        {
            return sortBy switch
            {
                "Popular" => PopularityRanking.ByPopularity(
                    products,
                    product => product.PopularityScore,
                    product => product.RealSalesCount,
                    product => product.Id).ToList(),
                "Price Low to High" => products.OrderBy(product => product.EffectivePrice).ThenBy(product => product.Id).ToList(),
                "Price High to Low" => products.OrderByDescending(product => product.EffectivePrice).ThenBy(product => product.Id).ToList(),
                "Name A-Z" => products.OrderBy(product => product.Name).ThenBy(product => product.Id).ToList(),
                "Name Z-A" => products.OrderByDescending(product => product.Name).ThenBy(product => product.Id).ToList(),
                "Rating" => products.OrderByDescending(product => product.Rating).ThenByDescending(product => product.RatingCount).ThenBy(product => product.Id).ToList(),
                _ => PopularityRanking.ByLatest(products, product => product.CreatedAt, product => product.Id).ToList()
            };
        }

        public async Task<IActionResult> Index(
            string? search,
            string? category,
            string? brand,
            int? discount,
            int? campaign,
            CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Shop - AutoVault";
            await EnsureCatalogAsync(cancellationToken);
            var discountPercent = discount is > 0 ? discount : null;
            var campaignId = campaign is > 0 ? campaign : null;
            var matched = OrderCatalog(
                MatchCatalog(search, category, brand, discountPercent, campaignId),
                "Latest");
            var initialProducts = matched.Take(CatalogPageSize).ToList();
            await ApplyRatingsAsync(initialProducts, cancellationToken);
            var response = new ShopFilterResponse
            {
                Products = initialProducts,
                TotalProducts = matched.Count,
                CurrentPage = 1,
                TotalPages = (int)Math.Ceiling(matched.Count / (double)CatalogPageSize),
                HasMorePages = matched.Count > CatalogPageSize,
                ResultsText = CatalogResultsText(matched.Count, CatalogPageSize, discountPercent, campaignId)
            };

            SetShopFilters();
            ViewBag.CategoryCounts = _allProducts
                .Where(product => !string.IsNullOrWhiteSpace(product.Category))
                .GroupBy(product => product.Category)
                .ToDictionary(group => group.Key, group => group.Count());
            ViewData["LiveCampaigns"] = await _shopCatalog.GetLiveCampaignsAsync(cancellationToken);
            ViewData["BodyClass"] = "home-four";
            ViewData["Search"] = search;
            ViewData["Category"] = category;
            ViewData["Brand"] = brand;
            ViewData["Discount"] = discountPercent;
            ViewData["CampaignId"] = campaignId;
            return View(response);
        }

        [HttpPost]
        public async Task<JsonResult> FilterProducts(
            [FromBody] ShopFilterRequest? request,
            CancellationToken cancellationToken)
        {
            request ??= new ShopFilterRequest();
            if (request.Page < 1)
            {
                request.Page = 1;
            }

            if (request.PageSize < 1)
            {
                request.PageSize = 6;
            }

            await EnsureCatalogAsync(cancellationToken);
            await ApplyRatingsAsync(_allProducts, cancellationToken);
            var filteredProducts = _allProducts.AsQueryable();

            // Apply search text filter (name, description, keywords)
            if (!string.IsNullOrEmpty(request.SearchText))
            {
                var searchLower = request.SearchText.ToLower();
                filteredProducts = filteredProducts.Where(p =>
                    p.Name.ToLower().Contains(searchLower) ||
                    p.Description.ToLower().Contains(searchLower) ||
                    p.Brand.ToLower().Contains(searchLower) ||
                    p.PartNumber.ToLower().Contains(searchLower) ||
                    p.CarMake.ToLower().Contains(searchLower) ||
                    p.CarModel.ToLower().Contains(searchLower) ||
                    (p.SearchKeywords != null && p.SearchKeywords.ToLower().Contains(searchLower))
                );
            }

            // Apply category filter
            if (!string.IsNullOrEmpty(request.Category))
            {
                filteredProducts = filteredProducts.Where(p => p.Category == request.Category);
            }

            if (!string.IsNullOrEmpty(request.Brand))
            {
                filteredProducts = filteredProducts.Where(p =>
                    string.Equals(p.Brand, request.Brand, StringComparison.OrdinalIgnoreCase));
            }

            if (request.CampaignId is int requestCampaignId and > 0)
            {
                filteredProducts = filteredProducts.Where(p => p.HasOffer && p.OfferCampaignId == requestCampaignId);
            }
            else if (request.DiscountPercent is int percent and > 0)
            {
                filteredProducts = filteredProducts.Where(p => p.HasOffer && p.DiscountPercent == percent);
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
                        filteredProducts = filteredProducts.Where(p => p.EffectivePrice < 200);
                        break;
                    case "Under $400":
                        filteredProducts = filteredProducts.Where(p => p.EffectivePrice < 400);
                        break;
                    case "Under $600":
                        filteredProducts = filteredProducts.Where(p => p.EffectivePrice < 600);
                        break;
                    case "Under $800":
                        filteredProducts = filteredProducts.Where(p => p.EffectivePrice < 800);
                        break;
                    case "All Prices":
                        // No filter applied
                        break;
                }
            }

            // Apply min/max price filter (slider from ShopTwo)
            if (request.MinPrice.HasValue)
            {
                filteredProducts = filteredProducts.Where(p => p.EffectivePrice >= request.MinPrice.Value);
            }

            if (request.MaxPrice.HasValue)
            {
                filteredProducts = filteredProducts.Where(p => p.EffectivePrice <= request.MaxPrice.Value);
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(request.SortBy) && request.SortBy != "Sort By")
            {
                switch (request.SortBy)
                {
                    case "Price Low to High":
                        filteredProducts = filteredProducts.OrderBy(p => p.EffectivePrice);
                        break;
                    case "Price High to Low":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.EffectivePrice);
                        break;
                    case "Name A-Z":
                        filteredProducts = filteredProducts.OrderBy(p => p.Name);
                        break;
                    case "Name Z-A":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.Name);
                        break;
                    case "Rating":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.Rating).ThenByDescending(p => p.RatingCount).ThenBy(p => p.Id);
                        break;
                    case "Popular":
                        filteredProducts = filteredProducts
                            .OrderByDescending(p => p.PopularityScore)
                            .ThenByDescending(p => p.RealSalesCount)
                            .ThenBy(p => p.Id);
                        break;
                    case "Latest":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id);
                        break;
                    default:
                        filteredProducts = filteredProducts.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id);
                        break;
                }
            }
            else
            {
                filteredProducts = filteredProducts.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id);
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
                    : request.CampaignId is > 0
                        ? "No active parts currently use this campaign offer."
                        : request.DiscountPercent is int emptyDiscount and > 0
                            ? $"No active parts currently have a {emptyDiscount}% discount."
                            : "No products found"
            };

            return Json(response);
        }

        [HttpPost]
        public async Task<JsonResult> LoadMore(
            int page = 1,
            int pageSize = CatalogPageSize,
            string? search = null,
            string? category = null,
            string? brand = null,
            string? sortBy = null,
            int? discount = null,
            int? campaign = null,
            CancellationToken cancellationToken = default)
        {
            await EnsureCatalogAsync(cancellationToken);
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 48);
            var discountPercent = discount is > 0 ? discount : null;
            var campaignId = campaign is > 0 ? campaign : null;
            var matched = OrderCatalog(
                MatchCatalog(search, category, brand, discountPercent, campaignId),
                sortBy);
            var products = matched
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            await ApplyRatingsAsync(products, cancellationToken);

            var totalPages = (int)Math.Ceiling(matched.Count / (double)pageSize);
            var endItem = Math.Min(page * pageSize, matched.Count);

            var response = new ShopFilterResponse
            {
                Products = products,
                TotalProducts = matched.Count,
                CurrentPage = page,
                TotalPages = totalPages,
                HasMorePages = page < totalPages,
                ResultsText = matched.Count > 0
                    ? $"Showing 1-{endItem} of {matched.Count} results"
                    : CatalogResultsText(0, pageSize, discountPercent, campaignId)
            };

            return Json(response);
        }

        public async Task<IActionResult> ShopTwo(CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Shop Two - AutoVault";
            await EnsureCatalogAsync(cancellationToken);
            
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
            await EnsureCatalogAsync(cancellationToken);
            
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
            await EnsureCatalogAsync(cancellationToken);
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
                        ViewBag.UserRating   = userRating?.Rating ?? 0;
                        ViewBag.UserRatingId = userRating?.Id ?? 0;
                        ViewBag.AskEmail = user.Email ?? string.Empty;
                        ViewBag.AskDisplayName = $"{user.FirstName} {user.LastName}".Trim();
                    }
                }
            }
            catch (CarApiException)
            {
                ViewData["RatingServiceUnavailable"] = true;
            }

            try
            {
                string? accessToken = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        accessToken = _tokenService.CreateToken(User, roles);
                    }
                }

                ViewBag.Reviews = await _reviewApiService.GetReviewsAsync(product.Id, accessToken, cancellationToken);
            }
            catch (CarApiException)
            {
                ViewBag.Reviews = Array.Empty<FinalProject.MVC.Models.Api.ProductReviewDto>();
                ViewData["ReviewServiceUnavailable"] = true;
            }

            var related = _allProducts
                .Where(item => item.Id != product.Id && item.Category == product.Category)
                .Concat(_allProducts.Where(item => item.Id != product.Id && item.Category != product.Category))
                .Take(4)
                .Select(CopyPart)
                .ToList();
            await ApplyRatingsAsync(related, cancellationToken);
            ViewBag.RelatedProducts = related;
            ViewBag.SubmissionKey = Guid.NewGuid();

            // Load answered inquiries so the product page can display admin replies
            var inquiries = await _shopCatalog.GetInquiriesAsync(status: null, cancellationToken);
            ViewBag.ProductInquiries = inquiries
                .Where(q => q.ProductId == product.Id && q.HasReply)
                .ToList();

            await MarkProductFavoriteAsync(product, cancellationToken);

            return View(product);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> AddFavorite(int id, string? returnUrl = null) =>
            SetProductFavoriteAsync(id, favorite: true, returnUrl);

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> RemoveFavorite(int id, string? returnUrl = null) =>
            SetProductFavoriteAsync(id, favorite: false, returnUrl);

        private async Task<IActionResult> SetProductFavoriteAsync(int id, bool favorite, string? returnUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            try
            {
                var roles = await _userManager.GetRolesAsync(user);
                var token = _tokenService.CreateToken(User, roles);
                if (favorite)
                {
                    await _marketplaceApi.AddProductFavoriteAsync(id, token, HttpContext.RequestAborted);
                }
                else
                {
                    await _marketplaceApi.RemoveProductFavoriteAsync(id, token, HttpContext.RequestAborted);
                }
            }
            catch (CarApiException exception)
            {
                TempData["FavoriteError"] = exception.Message;
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task MarkProductFavoriteAsync(ShopProduct product, CancellationToken cancellationToken)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return;
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return;
                }

                var roles = await _userManager.GetRolesAsync(user);
                var token = _tokenService.CreateToken(User, roles);
                var ids = await _marketplaceApi.GetProductFavoriteIdsAsync(token, cancellationToken);
                product.IsFavorite = ids.Contains(product.Id);
            }
            catch (CarApiException)
            {
                ViewData["FavoriteServiceUnavailable"] = true;
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SubmitRating(
            int productId,
            int rating,
            CancellationToken cancellationToken)
        {
            await EnsureCatalogAsync(cancellationToken);
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
                var saved = await _ratingApiService.SaveRatingAsync(
                    productId,
                    rating,
                    accessToken,
                    cancellationToken);
                var summary = await _ratingApiService.GetRatingsAsync(productId, cancellationToken);

                return Json(new
                {
                    success = true,
                    message = "Your rating has been saved.",
                    ratingId = saved.Id,
                    averageRating = summary?.AverageRating ?? rating,
                    totalCount = summary?.TotalCount ?? 1,
                    userRating = rating
                });
            }
            catch (CarApiException ex) when (ex.Message.Contains("already have a rating", StringComparison.OrdinalIgnoreCase)
                                              || ex.Message.Contains("409", StringComparison.Ordinal))
            {
                Response.StatusCode = StatusCodes.Status409Conflict;
                return Json(new
                {
                    success = false,
                    alreadyRated = true,
                    message = "You already rated this product. Delete your existing rating first, then you can submit a new one."
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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> DeleteMyRating(
            int productId,
            int ratingId,
            CancellationToken cancellationToken)
        {
            await EnsureCatalogAsync(cancellationToken);

            if (_allProducts.All(product => product.Id != productId))
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return Json(new { success = false, message = "Product not found." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Json(new { success = false, message = "Please sign in." });
            }

            try
            {
                var roles = await _userManager.GetRolesAsync(user);
                var accessToken = _tokenService.CreateToken(User, roles);
                await _ratingApiService.DeleteMyRatingAsync(productId, ratingId, accessToken, cancellationToken);
                var summary = await _ratingApiService.GetRatingsAsync(productId, cancellationToken);

                return Json(new
                {
                    success = true,
                    message = "Your rating has been removed. You can now submit a new one.",
                    averageRating = summary?.AverageRating ?? 0.0,
                    totalCount = summary?.TotalCount ?? 0
                });
            }
            catch (CarApiException ex) when (ex.Message.Contains("403", StringComparison.Ordinal)
                                              || ex.Message.Contains("Forbidden", StringComparison.OrdinalIgnoreCase))
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return Json(new { success = false, message = "You can only delete your own rating." });
            }
            catch (CarApiException ex) when (ex.Message.Contains("404", StringComparison.Ordinal)
                                              || ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return Json(new { success = false, message = "Rating not found." });
            }
            catch (CarApiException)
            {
                Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return Json(new { success = false, message = "Ratings are temporarily unavailable. Please try again." });
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveReview(
            int productId,
            int? reviewId,
            string content,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            try
            {
                var roles = await _userManager.GetRolesAsync(user);
                var accessToken = _tokenService.CreateToken(User, roles);
                await _reviewApiService.SaveReviewAsync(productId, reviewId, content, accessToken, cancellationToken);
                TempData["Success"] = reviewId.HasValue ? "Your review was updated." : "Your review was saved.";
            }
            catch (CarApiException exception)
            {
                TempData["Error"] = exception.Message;
            }

            return RedirectToAction(nameof(Details), new { id = productId });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int productId, int reviewId, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            try
            {
                var roles = await _userManager.GetRolesAsync(user);
                var accessToken = _tokenService.CreateToken(User, roles);
                await _reviewApiService.DeleteReviewAsync(reviewId, accessToken, cancellationToken);
                TempData["Success"] = "Your review was deleted.";
            }
            catch (CarApiException exception)
            {
                TempData["Error"] = exception.Message;
            }

            return RedirectToAction(nameof(Details), new { id = productId });
        }

        /// <summary>
        /// JSON endpoint used by the inline review editor on the product details page.
        /// Returns the saved ProductReviewDto so the client can update the DOM without a page reload.
        /// </summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveReviewJson(
            int productId,
            int? reviewId,
            string content,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { ok = false, error = "Not authenticated." });
            }

            try
            {
                var roles = await _userManager.GetRolesAsync(user);
                var accessToken = _tokenService.CreateToken(User, roles);
                var saved = await _reviewApiService.SaveReviewAsync(productId, reviewId, content, accessToken, cancellationToken);
                return Json(new
                {
                    ok = true,
                    review = new
                    {
                        saved.Id,
                        saved.Content,
                        saved.UserDisplayName,
                        saved.CreatedAt,
                        saved.UpdatedAt,
                        saved.CanEdit,
                        saved.IsHidden,
                        wasEdited = saved.WasEdited
                    }
                });
            }
            catch (CarApiException exception)
            {
                return Json(new { ok = false, error = exception.Message });
            }
        }

        public IActionResult Cart()
        {
            TempData["Success"] = "Questions about a part are sent from that product page. Our team reads them in the admin panel.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ask(ProductInquiry inquiry, CancellationToken cancellationToken)
        {
            // Never trust a posted UserId — bind ownership only from the signed-in account.
            inquiry.UserId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                inquiry.UserId = user?.Id;
                if (user != null)
                {
                    if (string.IsNullOrWhiteSpace(inquiry.Email) && !string.IsNullOrWhiteSpace(user.Email))
                    {
                        inquiry.Email = user.Email;
                    }

                    if (string.IsNullOrWhiteSpace(inquiry.CustomerName))
                    {
                        var display = $"{user.FirstName} {user.LastName}".Trim();
                        if (!string.IsNullOrWhiteSpace(display))
                        {
                            inquiry.CustomerName = display;
                        }
                    }
                }
            }

            var (ok, error) = await _shopCatalog.SaveInquiryAsync(inquiry, cancellationToken);
            TempData[ok ? "Success" : "Error"] = ok
                ? "Your question was sent to the store. Visit Babek Prospekti to inspect the item before you buy."
                : error;
            return RedirectToAction(nameof(Details), "Shop", new { id = inquiry.ProductId }, "ask-question");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestToBuy(ShowroomPurchaseRequest request, CancellationToken cancellationToken)
        {
            // Never trust a posted UserId — bind ownership only from the signed-in account.
            request.UserId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                request.UserId = user?.Id;
            }

            var result = await _shopCatalog.SavePurchaseRequestAsync(request, cancellationToken);
            if (result.Ok)
            {
                TempData["RequestSuccess"] = result.Duplicate
                    ? $"We already received this request (#{result.RequestId}). It was not sent twice."
                    : $"Request #{result.RequestId} sent. This is not a confirmed purchase. Our team will contact you to confirm availability, and you complete the purchase in person at Babek Prospekti 22-24, Baku.";
            }
            else
            {
                TempData["RequestError"] = result.Error;
                TempData["RequestForm"] = System.Text.Json.JsonSerializer.Serialize(new
                {
                    request.CustomerName,
                    request.Email,
                    request.Phone,
                    request.Quantity,
                    PreferredVisitDate = request.PreferredVisitDate?.ToString("yyyy-MM-dd"),
                    request.Note
                });
            }

            return RedirectToAction(nameof(Details), "Shop", new { id = request.ProductId }, "request-to-buy");
        }

        [HttpPost]
        public async Task<JsonResult> AddToCart([FromBody] AddToCartRequest request, CancellationToken cancellationToken)
        {
            await EnsureCatalogAsync(cancellationToken);
            var product = _allProducts.FirstOrDefault(p => p.Id == request.ProductId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            if (product.Quantity <= 0)
            {
                return Json(new { success = false, message = "This product is out of stock." });
            }

            var cart = GetCartFromSession();
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
            var nextQuantity = (existingItem?.Quantity ?? 0) + request.Quantity;
            if (request.Quantity < 1 || nextQuantity > product.Quantity)
            {
                return Json(new { success = false, message = "That quantity is not available." });
            }

            if (existingItem != null)
            {
                existingItem.Quantity = nextQuantity;
                existingItem.Price = product.EffectivePrice;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.EffectivePrice,
                    Quantity = request.Quantity,
                    ImagePath = product.ImagePath
                });
            }

            await RecalculateSessionCouponAsync(cart, cancellationToken);
            SaveCartToSession(cart);

            return Json(new
            {
                success = true,
                message = "Added to your inquiry list. Collect it at the Baku store.",
                cartCount = cart.Items.Sum(i => i.Quantity),
                cartTotal = cart.Total
            });
        }

        [HttpPost]
        public async Task<JsonResult> UpdateQuantity(
            [FromBody] UpdateQuantityRequest request,
            CancellationToken cancellationToken)
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
            await RecalculateSessionCouponAsync(cart, cancellationToken);
            SaveCartToSession(cart);

            return Json(new
            {
                success = true,
                itemSubtotal = item.Subtotal,
                cartSubtotal = cart.Subtotal,
                cartDiscount = cart.DiscountAmount,
                cartTotal = cart.Total,
                couponCode = cart.CouponCode
            });
        }

        [HttpPost]
        public async Task<JsonResult> RemoveItem(
            [FromBody] int productId,
            CancellationToken cancellationToken)
        {
            var cart = GetCartFromSession();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (item != null)
            {
                cart.Items.Remove(item);
                await RecalculateSessionCouponAsync(cart, cancellationToken);
                SaveCartToSession(cart);
            }

            return Json(new
            {
                success = true,
                cartSubtotal = cart.Subtotal,
                cartDiscount = cart.DiscountAmount,
                cartTotal = cart.Total,
                couponCode = cart.CouponCode,
                isEmpty = cart.IsEmpty
            });
        }

        [HttpPost]
        public async Task<JsonResult> ApplyCoupon(
            [FromBody] ApplyCouponRequest request,
            CancellationToken cancellationToken)
        {
            var cart = GetCartFromSession();

            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Json(new CouponResult
                {
                    Success = false,
                    Message = "Please sign in to use a coupon.",
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

            var (ok, message, discount, code) = await _coupons.PreviewApplyAsync(
                request.CouponCode,
                userId,
                cart,
                cancellationToken);

            if (!ok || code == null)
            {
                // Leave any previously applied session coupon untouched on failure.
                return Json(new CouponResult
                {
                    Success = false,
                    Message = message,
                    DiscountAmount = cart.DiscountAmount,
                    NewTotal = cart.Total
                });
            }

            cart.CouponCode = code;
            cart.DiscountAmount = discount;
            SaveCartToSession(cart);

            return Json(new CouponResult
            {
                Success = true,
                Message = message,
                DiscountAmount = cart.DiscountAmount,
                NewTotal = cart.Total
            });
        }

        [HttpPost]
        public JsonResult RemoveCoupon()
        {
            var cart = GetCartFromSession();
            cart.CouponCode = null;
            cart.DiscountAmount = 0;
            SaveCartToSession(cart);

            return Json(new CouponResult
            {
                Success = true,
                Message = "Coupon removed.",
                DiscountAmount = 0,
                NewTotal = cart.Total
            });
        }

        private async Task RecalculateSessionCouponAsync(CartViewModel cart, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(cart.CouponCode))
            {
                cart.DiscountAmount = 0;
                return;
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                cart.CouponCode = null;
                cart.DiscountAmount = 0;
                return;
            }

            var (ok, _, discount, code) = await _coupons.PreviewApplyAsync(
                cart.CouponCode,
                userId,
                cart,
                cancellationToken);
            if (!ok || code == null)
            {
                cart.CouponCode = null;
                cart.DiscountAmount = 0;
                return;
            }

            cart.CouponCode = code;
            cart.DiscountAmount = discount;
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
            TempData["Success"] = "Online checkout is not available. Visit the Baku store to buy in person.";
            return RedirectToAction("Index", "Contact");
        }
    }
}
