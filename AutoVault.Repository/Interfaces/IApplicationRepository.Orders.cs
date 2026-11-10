using FinalProject.MVC.Models;

namespace FinalProject.MVC.Repositories;

public partial interface IApplicationRepository
{
    void AddOrder(Order order);
    Task<Order?> GetOrderWithItemsByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetRecentOrdersWithItemsAsync(int take, CancellationToken cancellationToken = default);
}
