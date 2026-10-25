using System.Globalization;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Campaigns")]
[Route("[controller]")]
public sealed class AdminCampaignsController : Controller
{
    private readonly IShopCatalogService _catalog;
    private readonly ILogger<AdminCampaignsController> _logger;

    public AdminCampaignsController(IShopCatalogService catalog, ILogger<AdminCampaignsController> logger)
    {
        _catalog = catalog;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string? status, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Campaigns - AutoVault";
        var now = DateTime.UtcNow;
        var campaigns = await _catalog.GetCampaignsAsync(cancellationToken);
        var items = campaigns.Select(campaign => ToListItem(campaign, now)).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            items = items
                .Where(item =>
                    item.ProductName.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || item.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || item.ScopeLabel.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            items = items
                .Where(item => string.Equals(item.Status, status.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return View(new AdminCampaignsIndexViewModel
        {
            Items = items,
            Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
            TotalCount = items.Count
        });
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Campaign Details - AutoVault";
        var campaign = await _catalog.GetCampaignAsync(id, cancellationToken);
        if (campaign == null)
        {
            return NotFound();
        }

        return View(ToListItem(campaign, DateTime.UtcNow));
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Create Campaign - AutoVault";
        await LoadProducts(cancellationToken);
        var today = DateTime.UtcNow.Date;
        return View(new ProductCampaign
        {
            StartsAt = today,
            EndsAt = today.AddDays(14),
            IsActive = true,
            DiscountPercent = 15,
            AppliesToAllParts = false
        });
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Campaign - AutoVault";
        var campaign = await _catalog.GetCampaignAsync(id, cancellationToken);
        if (campaign == null)
        {
            return NotFound();
        }

        campaign.EndsAt = ShowroomRules.InclusiveEndUtc(campaign.EndsAt);
        if (!campaign.DiscountPercent.HasValue
            && campaign.DiscountedPrice.HasValue
            && campaign.Product != null
            && campaign.Product.Price > 0)
        {
            campaign.DiscountPercent = ShowroomRules.BadgePercent(
                campaign.Product.Price,
                campaign.DiscountedPrice.Value,
                null);
        }

        await LoadProducts(cancellationToken);
        return View(campaign);
    }

    [HttpPost("Save")]
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([FromForm] CampaignFormInput form, CancellationToken cancellationToken)
    {
        var activeValues = Request.HasFormContentType
            ? string.Join(",", Request.Form["isActive"].ToArray())
            : string.Empty;
        _logger.LogInformation(
            "Campaign POST {Path} Id={Id} Scope={Scope} ProductId={ProductId} Percent={Percent} Start={Start} End={End} ActiveValues={Active}",
            Request.Path,
            form.Id,
            form.AppliesToAllParts,
            form.ProductId,
            form.DiscountPercent,
            form.StartsAt,
            form.EndsAt,
            activeValues);

        var campaign = BindCampaign(form);
        if (!ModelState.IsValid)
        {
            _logger.LogWarning(
                "Campaign POST rejected by validation: {Errors}",
                string.Join("; ", ModelState.Values.SelectMany(value => value.Errors).Select(error => error.ErrorMessage)));
            return await RedisplayFormAsync(campaign, cancellationToken);
        }

        try
        {
            _logger.LogInformation("Campaign POST saving name={Name} allParts={All} product={Product} percent={Percent}",
                campaign.Name, campaign.AppliesToAllParts, campaign.ProductId, campaign.DiscountPercent);
            var (ok, error) = await _catalog.SaveCampaignAsync(campaign, cancellationToken);
            if (!ok)
            {
                _logger.LogWarning("Campaign save returned false: {Error}", error);
                ModelState.AddModelError(string.Empty, error ?? "The campaign could not be saved.");
                RestoreInclusiveFormDates(campaign);
                return await RedisplayFormAsync(campaign, cancellationToken);
            }

            if (campaign.Id <= 0)
            {
                _logger.LogError("Campaign save reported success but Id remained {Id}", campaign.Id);
                ModelState.AddModelError(string.Empty, "The campaign was not assigned an id after saving.");
                RestoreInclusiveFormDates(campaign);
                return await RedisplayFormAsync(campaign, cancellationToken);
            }

            _logger.LogInformation("Campaign saved Id={Id}", campaign.Id);
            TempData["Success"] =
                "The campaign was saved. It advertises an in-store spare-parts discount only — no online checkout.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Campaign save threw after POST to {Path}", Request.Path);
            ModelState.AddModelError(string.Empty, "The campaign could not be saved. " + exception.Message);
            RestoreInclusiveFormDates(campaign);
            return await RedisplayFormAsync(campaign, cancellationToken);
        }
    }

    [HttpPost("Active/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Active(int id, [FromForm] bool isActive, CancellationToken cancellationToken)
    {
        var (ok, error) = await _catalog.SetCampaignActiveAsync(id, isActive, cancellationToken);
        if (!ok)
        {
            TempData["Error"] = error ?? "The campaign could not be updated.";
            return RedirectToAction(nameof(Index));
        }

        if (!isActive)
        {
            TempData["Success"] = "The campaign is inactive.";
            return RedirectToAction(nameof(Index));
        }

        var campaign = await _catalog.GetCampaignAsync(id, cancellationToken);
        if (campaign == null)
        {
            TempData["Success"] = "The campaign is active.";
            return RedirectToAction(nameof(Index));
        }

        var status = ShowroomRules.CampaignPublicStatus(
            campaign.IsActive,
            campaign.StartsAt,
            campaign.EndsAt,
            DateTime.UtcNow);
        TempData["Success"] = status switch
        {
            "Live" => "The campaign is active and Live in the Shop.",
            "Scheduled" =>
                "The campaign is active, but it is Scheduled. It becomes Live when its UTC start date is reached.",
            "Expired" =>
                "The campaign is active, but its UTC date window has expired, so it is not Live in the Shop. Update the dates to make it Live.",
            _ => "The campaign is active."
        };
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var (ok, error) = await _catalog.DeleteCampaignAsync(id, cancellationToken);
        if (!ok)
        {
            TempData["Error"] = error ?? "The campaign could not be deleted.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "The campaign was deleted. Product prices and stock were not changed.";
        return RedirectToAction(nameof(Index));
    }

    private ProductCampaign BindCampaign(CampaignFormInput form)
    {
        var allParts = string.Equals(form.AppliesToAllParts, "true", StringComparison.OrdinalIgnoreCase);
        var isActive = Request.Form["isActive"]
            .Any(value => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));

        var campaign = new ProductCampaign
        {
            Id = form.Id,
            Name = form.Name ?? string.Empty,
            AppliesToAllParts = allParts,
            ProductId = allParts ? null : ParseProductId(form.ProductId),
            IsActive = isActive,
            DiscountedPrice = null
        };

        if (DateTime.TryParseExact(
                form.StartsAt,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var startDate))
        {
            campaign.StartsAt = startDate;
        }
        else
        {
            ModelState.AddModelError("StartsAt", "Enter a valid start date (UTC).");
        }

        if (DateTime.TryParseExact(
                form.EndsAt,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var endDate))
        {
            campaign.EndsAt = endDate;
        }
        else
        {
            ModelState.AddModelError("EndsAt", "Enter a valid inclusive end date (UTC).");
        }

        if (TryParsePercent(form.DiscountPercent, out var percent))
        {
            campaign.DiscountPercent = percent;
        }
        else
        {
            ModelState.AddModelError("DiscountPercent", "Enter a discount percentage greater than 0 and at most 100.");
        }

        if (!allParts && campaign.ProductId is null or <= 0)
        {
            ModelState.AddModelError("ProductId", "Select a spare part.");
        }

        return campaign;
    }

    private static int? ParseProductId(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id > 0
            ? id
            : null;
    }

    private static bool TryParsePercent(string? value, out decimal percent)
    {
        percent = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var text = value.Trim().Replace(',', '.');
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out percent)
            && !decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out percent))
        {
            return false;
        }

        return percent > 0 && percent <= 100;
    }

    private async Task<IActionResult> RedisplayFormAsync(ProductCampaign campaign, CancellationToken cancellationToken)
    {
        await LoadProducts(cancellationToken);
        return View(campaign.Id == 0 ? "Create" : "Edit", campaign);
    }

    private static void RestoreInclusiveFormDates(ProductCampaign campaign)
    {
        campaign.EndsAt = ShowroomRules.InclusiveEndUtc(campaign.EndsAt);
    }

    private async Task LoadProducts(CancellationToken cancellationToken)
    {
        ViewBag.Products = (await _catalog.GetAllAsync(cancellationToken))
            .OrderBy(product => product.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static CampaignListItem ToListItem(ProductCampaign campaign, DateTime utcNow)
    {
        decimal? original = campaign.AppliesToAllParts ? null : campaign.Product?.Price;
        decimal? sale = null;
        decimal? percent = campaign.DiscountPercent;
        if (original is decimal price and > 0)
        {
            sale = ShowroomRules.DiscountedPrice(price, campaign.DiscountedPrice, campaign.DiscountPercent);
            percent = ShowroomRules.BadgePercent(price, sale.Value, campaign.DiscountPercent);
        }

        return new CampaignListItem
        {
            Id = campaign.Id,
            Name = campaign.Name,
            AppliesToAllParts = campaign.AppliesToAllParts,
            ScopeLabel = campaign.AppliesToAllParts ? "All spare parts" : "Single spare part",
            ProductId = campaign.ProductId,
            ProductName = campaign.AppliesToAllParts
                ? "All spare parts"
                : (campaign.Product?.Name ?? $"Product #{campaign.ProductId}"),
            OriginalPrice = original,
            DiscountedPrice = sale,
            DiscountPercent = percent,
            StartsAt = campaign.StartsAt,
            EndsAtInclusive = ShowroomRules.InclusiveEndUtc(campaign.EndsAt),
            IsActive = campaign.IsActive,
            Status = ShowroomRules.CampaignPublicStatus(campaign.IsActive, campaign.StartsAt, campaign.EndsAt, utcNow)
        };
    }
}
