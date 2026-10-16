using Microsoft.AspNetCore.Mvc;
using FinalProject.MVC.Services.Interfaces;

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
            TempData["Success"] = "AutoVault does not take online payment. Call the store, send an inquiry, or visit Babek Prospekti 22–24 in Baku.";
            return RedirectToAction("Index", "Contact");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PlaceOrder()
        {
            TempData["Success"] = "Online checkout is not available. Visit the Baku store to buy in person.";
            return RedirectToAction("Index", "Contact");
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
    }
}
