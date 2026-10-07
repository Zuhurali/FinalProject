using Microsoft.AspNetCore.Mvc;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using System.Text.Json;

namespace FinalProject.MVC.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly IOrderService _orders;

        public CheckoutController(IOrderService orders)
        {
            _orders = orders;
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

            try
            {
                var order = await _orders.PlaceOrderAsync(
                    userId,
                    firstName,
                    lastName,
                    email,
                    phoneNumber,
                    streetAddress,
                    city,
                    district,
                    postCode,
                    companyName,
                    orderNotes,
                    cart);

                ClearCart();

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
        public async Task<IActionResult> Confirmation(int id)
        {
            ViewData["Title"] = "Order Confirmation - AutoVault";

            var order = await _orders.GetOrderAsync(id);

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
