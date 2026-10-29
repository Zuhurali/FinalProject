using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Orders")]
public sealed class AdminOrdersController : Controller
{
    private readonly IOrderService _orders;

    public AdminOrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Legacy Order Archive - AutoVault";
        return View(await _orders.GetRecentOrdersAsync(cancellationToken: cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = $"Legacy order #{id} - AutoVault";
        var order = await _orders.GetOrderAsync(id, cancellationToken);
        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }
}
