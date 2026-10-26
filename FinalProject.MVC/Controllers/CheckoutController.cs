using FinalProject.MVC.Security;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        /// <summary>
        /// Legacy order confirmation. Online checkout no longer creates orders; if a row exists,
        /// only the owning user (or Admin/SuperAdmin) may view it. Others get 404 without PII.
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Confirmation(int id, CancellationToken cancellationToken)
        {
            var order = await _orders.GetOrderAsync(id, cancellationToken);
            if (order == null || !OrderConfirmationAccess.CanView(order.UserId, User))
            {
                return NotFound();
            }

            ViewData["Title"] = "Order Confirmation - AutoVault";
            return View(order);
        }
    }
}
