using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public class CarLoanCalculatorModel
{
    [Display(Name = "Car make")]
    public string? CarMake { get; set; }

    [Display(Name = "Car price")]
    public string? CarPrice { get; set; }

    [Display(Name = "Down payment")]
    public string? DownPayment { get; set; }

    [Display(Name = "Loan term (months)")]
    public string? LoanTermMonths { get; set; }

    [Display(Name = "Annual interest rate (%)")]
    public string? AnnualInterestRate { get; set; } = "8";

    /// <summary>Brand names from the API Brands table (SQL), ordered for the make dropdown.</summary>
    public IReadOnlyList<string> BrandNames { get; set; } = Array.Empty<string>();
}
