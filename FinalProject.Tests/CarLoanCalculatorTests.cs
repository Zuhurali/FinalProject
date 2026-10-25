using FinalProject.MVC.Commerce;
using Xunit;

namespace FinalProject.Tests;

public class CarLoanCalculatorTests
{
    [Fact]
    public void TestA_eight_percent_matches_expected_amortization()
    {
        var ok = CarLoanCalculator.TryCalculate(new CarLoanCalculatorInput
        {
            CarMake = "Honda",
            CarPrice = "30000",
            DownPayment = "15000",
            LoanTermMonths = "36",
            AnnualInterestRate = "8"
        }, out var result, out var errors);

        Assert.True(ok);
        Assert.Empty(errors);
        Assert.NotNull(result);
        Assert.Equal(15000m, result!.Principal);
        Assert.Equal(470.05m, result.MonthlyPayment);
        Assert.Equal(16921.80m, result.TotalLoanPayments);
        Assert.Equal(1921.80m, result.TotalInterest);
        Assert.Equal(31921.80m, result.TotalCostIncludingDownPayment);
    }

    [Fact]
    public void TestB_zero_percent_uses_simple_division_without_invented_interest()
    {
        var ok = CarLoanCalculator.TryCalculate(new CarLoanCalculatorInput
        {
            CarMake = "Mazda",
            CarPrice = "30000",
            DownPayment = "15000",
            LoanTermMonths = "36",
            AnnualInterestRate = "0"
        }, out var result, out var errors);

        Assert.True(ok);
        Assert.Empty(errors);
        Assert.NotNull(result);
        Assert.Equal(15000m, result!.Principal);
        Assert.Equal(416.67m, result.MonthlyPayment);
        Assert.Equal(15000m, result.TotalLoanPayments);
        Assert.Equal(0m, result.TotalInterest);
        Assert.Equal(30000m, result.TotalCostIncludingDownPayment);
    }

    [Fact]
    public void Down_payment_equal_to_price_yields_zero_loan()
    {
        var ok = CarLoanCalculator.TryCalculate(new CarLoanCalculatorInput
        {
            CarMake = "Other",
            CarPrice = "25000",
            DownPayment = "25000",
            LoanTermMonths = "48",
            AnnualInterestRate = "6.5"
        }, out var result, out var errors);

        Assert.True(ok);
        Assert.Empty(errors);
        Assert.NotNull(result);
        Assert.Equal(0m, result!.Principal);
        Assert.Equal(0m, result.MonthlyPayment);
        Assert.Equal(0m, result.TotalLoanPayments);
        Assert.Equal(0m, result.TotalInterest);
        Assert.Equal(25000m, result.TotalCostIncludingDownPayment);
    }

    [Theory]
    [InlineData(null, "30000", "1000", "36", "8", "carMake")]
    [InlineData("Honda", "", "1000", "36", "8", "carPrice")]
    [InlineData("Honda", "abc", "1000", "36", "8", "carPrice")]
    [InlineData("Honda", "-5", "1000", "36", "8", "carPrice")]
    [InlineData("Honda", "0", "0", "36", "8", "carPrice")]
    [InlineData("Honda", "30000", "", "36", "8", "downPayment")]
    [InlineData("Honda", "30000", "35000", "36", "8", "downPayment")]
    [InlineData("Honda", "30000", "1000", "0", "8", "loanTermMonths")]
    [InlineData("Honda", "30000", "1000", "abc", "8", "loanTermMonths")]
    [InlineData("Honda", "30000", "1000", "36", "", "annualInterestRate")]
    [InlineData("Honda", "30000", "1000", "36", "rate", "annualInterestRate")]
    [InlineData("Honda", "30000", "1,000", "36", "8", "downPayment")]
    public void Invalid_input_is_rejected_without_silent_zero(
        string? make,
        string? price,
        string? down,
        string? term,
        string? rate,
        string expectedField)
    {
        var ok = CarLoanCalculator.TryCalculate(new CarLoanCalculatorInput
        {
            CarMake = make,
            CarPrice = price,
            DownPayment = down,
            LoanTermMonths = term,
            AnnualInterestRate = rate
        }, out var result, out var errors);

        Assert.False(ok);
        Assert.Null(result);
        Assert.True(errors.ContainsKey(expectedField), string.Join(", ", errors.Keys));
    }

    [Fact]
    public void Decimal_values_are_accepted_with_period_separator()
    {
        var ok = CarLoanCalculator.TryCalculate(new CarLoanCalculatorInput
        {
            CarMake = "Peugeot",
            CarPrice = "29999.99",
            DownPayment = "5000.50",
            LoanTermMonths = "60",
            AnnualInterestRate = "7.25"
        }, out var result, out var errors);

        Assert.True(ok);
        Assert.Empty(errors);
        Assert.NotNull(result);
        Assert.Equal(24999.49m, result!.Principal);
        Assert.True(result.MonthlyPayment > 0m);
        Assert.True(result.TotalInterest > 0m);
        Assert.Equal(result.TotalLoanPayments + result.DownPayment, result.TotalCostIncludingDownPayment);
    }
}
