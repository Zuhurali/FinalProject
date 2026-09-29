using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FinalProject.MVC.Models;
using FinalProject.MVC.Data;
using System.Text.Json;

namespace FinalProject.MVC.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CheckoutController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Checkout - AutoVault";

            // Get cart from session
            var cart = GetCartFromSession();

            // Redirect to cart if cart is empty
            if (cart.IsEmpty)
            {
                TempData["CheckoutError"] = "Your cart is empty. Please add items before checkout.";
                return RedirectToAction("Cart", "Shop");
            }

            // Pre-fill user info if logged in
            if (User.Identity?.IsAuthenticated ?? false)
            {
                ViewBag.UserEmail = User.Identity.Name;
                ViewBag.IsAuthenticated = true;
            }
            else
            {
                ViewBag.IsAuthenticated = false;
            }

            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(
            string firstName,
            string lastName,
            string email,
            string phoneNumber,
            string streetAddress,
            string city,
            string district,
            string? postCode,
            string? companyName,
            string? orderNotes)
        {
            // Get cart from session
            var cart = GetCartFromSession();

            // Validate cart
            if (cart.IsEmpty)
            {
                TempData["CheckoutError"] = "Your cart is empty. Please add items before checkout.";
                return RedirectToAction("Cart", "Shop");
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(phoneNumber) ||
                string.IsNullOrWhiteSpace(streetAddress) ||
                string.IsNullOrWhiteSpace(city) ||
                string.IsNullOrWhiteSpace(district))
            {
                TempData["Error"] = "Please fill in all required fields.";
                return RedirectToAction("Index");
            }

            // Get UserId if authenticated
            string? userId = null;
            if (User.Identity?.IsAuthenticated ?? false)
            {
                userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            }

            // Create order
            var order = new Order
            {
                UserId = userId,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber,
                StreetAddress = streetAddress,
                City = city,
                District = district,
                PostCode = postCode,
                CompanyName = companyName,
                OrderNotes = orderNotes,
                Subtotal = cart.Subtotal,
                DiscountAmount = cart.DiscountAmount,
                CouponCode = cart.CouponCode,
                Total = cart.Total,
                OrderDate = DateTime.UtcNow,
                OrderStatus = "Pending"
            };

            // Add order items
            foreach (var cartItem in cart.Items)
            {
                var orderItem = new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.ProductName,
                    Price = cartItem.Price,
                    Quantity = cartItem.Quantity,
                    Subtotal = cartItem.Subtotal,
                    ImagePath = cartItem.ImagePath
                };
                order.OrderItems.Add(orderItem);
            }

            try
            {
                // Save order to database
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Clear cart ONLY after successful order creation
                ClearCart();

                // Redirect to confirmation page
                return RedirectToAction("Confirmation", new { id = order.Id });
            }
            catch (Exception)
            {
                // Log the exception (in production, use proper logging)
                TempData["Error"] = "An error occurred while processing your order. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult Confirmation(int id)
        {
            ViewData["Title"] = "Order Confirmation - AutoVault";

            var order = _context.Orders
                .Where(o => o.Id == id)
                .Select(o => new
                {
                    o.Id,
                    o.OrderDate,
                    o.FirstName,
                    o.LastName,
                    o.Email,
                    o.PhoneNumber,
                    o.StreetAddress,
                    o.City,
                    o.District,
                    o.PostCode,
                    o.Subtotal,
                    o.DiscountAmount,
                    o.CouponCode,
                    o.Total,
                    OrderItems = o.OrderItems.Select(oi => new
                    {
                        oi.ProductName,
                        oi.Price,
                        oi.Quantity,
                        oi.Subtotal,
                        oi.ImagePath
                    }).ToList()
                })
                .FirstOrDefault();

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }

        // Helper methods
        private CartViewModel GetCartFromSession()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            return cartJson == null
                ? new CartViewModel()
                : JsonSerializer.Deserialize<CartViewModel>(cartJson) ?? new CartViewModel();
        }

        private void ClearCart()
        {
            HttpContext.Session.Remove("Cart");
        }
    }
}
