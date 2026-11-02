using FinalProject.MVC.Commerce;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class CalculatorController : Controller
    {
        private readonly ICalculatorPageContentService _page;
        private readonly ICarApiService _carApi;
        private readonly ILogger<CalculatorController> _logger;

        public CalculatorController(
            ICalculatorPageContentService page,
            ICarApiService carApi,
            ILogger<CalculatorController> logger)
        {
            _page = page;
            _carApi = carApi;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var page = _page.GetPublic();
            ViewData["Title"] = page.SeoTitle;
            ViewData["MetaDescription"] = page.SeoDescription;
            ViewData["CalculatorPage"] = page;

            var model = new CarLoanCalculatorModel
            {
                BrandNames = await LoadBrandNamesAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Produces("application/json")]
        public IActionResult Calculate([FromForm] CarLoanCalculatorInput input)
        {
            if (!CarLoanCalculator.TryCalculate(input, out var result, out var errors))
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Please correct the highlighted fields.",
                    errors
                })
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            return Json(new
            {
                success = true,
                message = "Loan estimate calculated.",
                result = new
                {
                    carMake = result!.CarMake,
                    carPrice = result.CarPrice,
                    downPayment = result.DownPayment,
                    loanTermMonths = result.LoanTermMonths,
                    annualInterestRate = result.AnnualInterestRate,
                    principal = result.Principal,
                    monthlyPayment = result.MonthlyPayment,
                    totalLoanPayments = result.TotalLoanPayments,
                    totalInterest = result.TotalInterest,
                    totalCostIncludingDownPayment = result.TotalCostIncludingDownPayment
                }
            });
        }

        private async Task<IReadOnlyList<string>> LoadBrandNamesAsync()
        {
            try
            {
                var lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted);
                return lookups.Brands
                    .Select(brand => brand.Name?.Trim() ?? string.Empty)
                    .Where(name => !string.IsNullOrWhiteSpace(name)
                        && !name.Equals("Other", StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (CarApiException exception)
            {
                _logger.LogWarning(exception, "Calculator brand list could not be loaded from SQL.");
                return Array.Empty<string>();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Calculator brand list could not be loaded.");
                return Array.Empty<string>();
            }
        }
    }
}
