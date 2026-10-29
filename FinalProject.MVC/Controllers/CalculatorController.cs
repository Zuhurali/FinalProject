using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class CalculatorController : Controller
    {
        private readonly ICalculatorPageContentService _page;

        public CalculatorController(ICalculatorPageContentService page)
        {
            _page = page;
        }

        public IActionResult Index()
        {
            var page = _page.GetPublic();
            ViewData["Title"] = page.SeoTitle;
            ViewData["MetaDescription"] = page.SeoDescription;
            ViewData["CalculatorPage"] = page;
            return View(new CarLoanCalculatorModel());
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
    }
}
