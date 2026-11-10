using System.Globalization;
using System.Text.RegularExpressions;

namespace FinalProject.MVC.Commerce;

public sealed class CarLoanCalculatorInput
{
    public string? CarMake { get; set; }
    public string? CarPrice { get; set; }
    public string? DownPayment { get; set; }
    public string? LoanTermMonths { get; set; }
    public string? AnnualInterestRate { get; set; }
}

public sealed class CarLoanCalculationResult
{
    public string CarMake { get; init; } = string.Empty;
    public decimal CarPrice { get; init; }
    public decimal DownPayment { get; init; }
    public int LoanTermMonths { get; init; }
    public decimal AnnualInterestRate { get; init; }
    public decimal Principal { get; init; }
    public decimal MonthlyPayment { get; init; }
    public decimal TotalLoanPayments { get; init; }
    public decimal TotalInterest { get; init; }
    public decimal TotalCostIncludingDownPayment { get; init; }
}

/// <summary>
/// Fixed-rate car loan amortization with explicit validation (no silent zero coercion).
/// </summary>
public static class CarLoanCalculator
{
    public const decimal MaxCarPrice = 10_000_000m;
    public const int MinLoanTermMonths = 1;
    public const int MaxLoanTermMonths = 120;
    public const decimal MaxAnnualInterestRate = 100m;

    private static readonly Regex MoneyPattern = new(@"^\d+(\.\d{1,2})?$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex RatePattern = new(@"^\d+(\.\d{1,4})?$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex WholeNumberPattern = new(@"^\d+$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool TryCalculate(
        CarLoanCalculatorInput input,
        out CarLoanCalculationResult? result,
        out Dictionary<string, string> errors)
    {
        result = null;
        errors = ValidateAndParse(input, out var carMake, out var price, out var down, out var months, out var annualRate);
        if (errors.Count > 0)
        {
            return false;
        }

        var principal = price - down;
        decimal monthlyPayment;
        decimal totalLoanPayments;
        decimal totalInterest;

        if (principal == 0m)
        {
            monthlyPayment = 0m;
            totalLoanPayments = 0m;
            totalInterest = 0m;
        }
        else if (annualRate == 0m)
        {
            monthlyPayment = RoundMoney(principal / months);
            totalLoanPayments = principal;
            totalInterest = 0m;
        }
        else
        {
            var monthlyRate = annualRate / 100m / 12m;
            var factor = Pow(1m + monthlyRate, months);
            monthlyPayment = RoundMoney(principal * monthlyRate * factor / (factor - 1m));
            totalLoanPayments = RoundMoney(monthlyPayment * months);
            totalInterest = RoundMoney(totalLoanPayments - principal);
        }

        var totalCost = RoundMoney(totalLoanPayments + down);

        result = new CarLoanCalculationResult
        {
            CarMake = carMake,
            CarPrice = price,
            DownPayment = down,
            LoanTermMonths = months,
            AnnualInterestRate = annualRate,
            Principal = principal,
            MonthlyPayment = monthlyPayment,
            TotalLoanPayments = totalLoanPayments,
            TotalInterest = totalInterest,
            TotalCostIncludingDownPayment = totalCost
        };

        return true;
    }

    public static Dictionary<string, string> Validate(CarLoanCalculatorInput input)
        => ValidateAndParse(input, out _, out _, out _, out _, out _);

    private static Dictionary<string, string> ValidateAndParse(
        CarLoanCalculatorInput input,
        out string carMake,
        out decimal price,
        out decimal down,
        out int months,
        out decimal annualRate)
    {
        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        carMake = (input.CarMake ?? string.Empty).Trim();
        price = 0m;
        down = 0m;
        months = 0;
        annualRate = 0m;

        if (string.IsNullOrWhiteSpace(carMake))
        {
            errors["carMake"] = "Select a car make.";
        }

        if (!TryParseMoney(input.CarPrice, out price))
        {
            errors["carPrice"] = string.IsNullOrWhiteSpace(input.CarPrice)
                ? "Enter the car price."
                : "Enter a valid car price using digits and an optional decimal point.";
        }
        else if (price <= 0m)
        {
            errors["carPrice"] = "Car price must be greater than zero.";
        }
        else if (price > MaxCarPrice)
        {
            errors["carPrice"] = $"Car price cannot exceed {MaxCarPrice:0}.";
        }

        if (!TryParseMoney(input.DownPayment, out down))
        {
            errors["downPayment"] = string.IsNullOrWhiteSpace(input.DownPayment)
                ? "Enter the down payment."
                : "Enter a valid down payment using digits and an optional decimal point.";
        }
        else if (down < 0m)
        {
            errors["downPayment"] = "Down payment cannot be negative.";
        }

        if (!errors.ContainsKey("carPrice") && !errors.ContainsKey("downPayment") && down > price)
        {
            errors["downPayment"] = "Down payment cannot be greater than the car price.";
        }

        if (!TryParseWholeNumber(input.LoanTermMonths, out months))
        {
            errors["loanTermMonths"] = string.IsNullOrWhiteSpace(input.LoanTermMonths)
                ? "Enter the loan term in months."
                : "Loan term must be a whole number of months.";
        }
        else if (months < MinLoanTermMonths || months > MaxLoanTermMonths)
        {
            errors["loanTermMonths"] = $"Loan term must be between {MinLoanTermMonths} and {MaxLoanTermMonths} months.";
        }

        if (!TryParseRate(input.AnnualInterestRate, out annualRate))
        {
            errors["annualInterestRate"] = string.IsNullOrWhiteSpace(input.AnnualInterestRate)
                ? "Enter the annual interest rate."
                : "Enter a valid annual interest rate using digits and an optional decimal point.";
        }
        else if (annualRate < 0m)
        {
            errors["annualInterestRate"] = "Annual interest rate cannot be negative.";
        }
        else if (annualRate > MaxAnnualInterestRate)
        {
            errors["annualInterestRate"] = $"Annual interest rate cannot exceed {MaxAnnualInterestRate:0}%.";
        }

        return errors;
    }

    private static bool TryParseMoney(string? raw, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var text = raw.Trim();
        if (!MoneyPattern.IsMatch(text))
        {
            return false;
        }

        return decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseRate(string? raw, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var text = raw.Trim();
        if (!RatePattern.IsMatch(text))
        {
            return false;
        }

        return decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseWholeNumber(string? raw, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var text = raw.Trim();
        if (!WholeNumberPattern.IsMatch(text))
        {
            return false;
        }

        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

    private static decimal RoundMoney(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static decimal Pow(decimal baseValue, int exponent)
    {
        decimal result = 1m;
        for (var i = 0; i < exponent; i++)
        {
            result *= baseValue;
        }

        return result;
    }
}
