using FinalProject.MVC.Models;

namespace FinalProject.MVC.Services.Interfaces;

public interface IProductPopularityService
{
    Task ApplyAsync(IList<ShopProduct> products, CancellationToken cancellationToken = default);
}
