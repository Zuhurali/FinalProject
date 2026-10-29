using FinalProject.MVC.Models;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Mappings;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

public class DealerController : Controller
{
    private readonly IDealerService _dealers;
    private readonly IDealerPageContentService _dealerPage;
    private readonly ICarApiService _carApi;

    public DealerController(
        IDealerService dealers,
        IDealerPageContentService dealerPage,
        ICarApiService carApi)
    {
        _dealers = dealers;
        _dealerPage = dealerPage;
        _carApi = carApi;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var page = _dealerPage.GetPublic();
        ViewData["Title"] = page.SeoTitle;
        ViewData["MetaDescription"] = page.SeoDescription;
        ViewData["DealerPage"] = page;
        return View(await _dealers.GetPublishedAsync(cancellationToken));
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var dealer = await _dealers.GetByIdAsync(id, cancellationToken);
        if (dealer == null || !dealer.IsPublished)
        {
            return NotFound();
        }

        var related = new List<Car>();
        try
        {
            var cars = (await CarApiMapper.LoadCarsAsync(_carApi, cancellationToken)).ToList();
            if (cars.Count > 0)
            {
                var start = ((id - 1) * 2) % cars.Count;
                related.Add(cars[start]);
                related.Add(cars[(start + 1) % cars.Count]);
            }
        }
        catch (CarApiException exception)
        {
            ViewData["CarApiError"] = exception.Message;
        }

        ViewData["Title"] = $"{dealer.Name} - AutoVault";
        var meta = string.IsNullOrWhiteSpace(dealer.Summary) ? dealer.Description : dealer.Summary;
        if (!string.IsNullOrWhiteSpace(meta))
        {
            meta = meta.Trim();
            ViewData["MetaDescription"] = meta.Length > 320 ? meta[..317] + "..." : meta;
        }

        return View(new DealerDetailsViewModel
        {
            Dealer = dealer,
            RelatedCars = related
        });
    }
}
