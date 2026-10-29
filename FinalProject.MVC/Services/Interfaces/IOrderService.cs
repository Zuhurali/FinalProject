using FinalProject.MVC.Models;

namespace FinalProject.MVC.Services.Interfaces;

public interface IOrderService
{
    /// <summary>
    /// Legacy online-order writer. Not invoked by public MVC (checkout redirects to Contact).
    /// Kept so historical <c>Orders</c> rows remain readable via Admin / Confirmation.
    /// </summary>
    Task<Order> PlaceOrderAsync(
        string? userId,
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string streetAddress,
        string city,
        string district,
        string? postCode,
        string? companyName,
        string? orderNotes,
        CartViewModel cart,
        CancellationToken cancellationToken = default);

    Task<Order?> GetOrderAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Recent checkout orders from SQL (Admin read surface).</summary>
    Task<IReadOnlyList<Order>> GetRecentOrdersAsync(int take = 100, CancellationToken cancellationToken = default);
}
