using FinalProject.MVC.Commerce;
using Xunit;

namespace FinalProject.Tests;

public class InternationalPhoneNumberTests
{
    [Theory]
    [InlineData("AZ", "501234567", "+994501234567")]
    [InlineData("AZ", "516546565", "+994516546565")]
    [InlineData("AZ", "+994501234567", "+994501234567")]
    [InlineData("AZ", "+994 50 123 45 67", "+994501234567")]
    [InlineData("TR", "5321234567", "+905321234567")]
    [InlineData("US", "5551234567", "+15551234567")]
    [InlineData("US", "+1 555 123 4567", "+15551234567")]
    [InlineData("OTHER", "+4915112345678", "+4915112345678")]
    public void Known_country_numbers_normalize_to_e164(string country, string input, string expected)
    {
        Assert.True(InternationalPhoneNumber.TryNormalize(country, input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("AZ", "+99456655151515151561511")]
    [InlineData("AZ", "0501234567")]
    [InlineData("AZ", "513183272222")]
    [InlineData("AZ", "50123456")]
    [InlineData("TR", "05321234567")]
    [InlineData("US", "55512345678")]
    [InlineData("OTHER", "+99456655151515151561511")]
    public void Oversized_or_short_numbers_are_rejected(string country, string input)
    {
        Assert.False(InternationalPhoneNumber.TryNormalize(country, input, out var normalized));
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void Azerbaijan_national_input_is_exactly_nine_digits()
    {
        Assert.Equal(9, InternationalPhoneNumber.Azerbaijan.MaxInputDigits);
        Assert.Equal("+994", InternationalPhoneNumber.Azerbaijan.PrefixDisplay);
        Assert.Equal("Azerbaijan (+994)", InternationalPhoneNumber.Azerbaijan.OptionLabel);
    }

    [Fact]
    public void Country_list_includes_russia_and_other_common_destinations()
    {
        Assert.Contains(InternationalPhoneNumber.Countries, c => c.Code == "RU");
        Assert.Contains(InternationalPhoneNumber.Countries, c => c.Code == "GB");
        Assert.Contains(InternationalPhoneNumber.Countries, c => c.Code == "OTHER");
        Assert.True(InternationalPhoneNumber.TryNormalize("RU", "9123456789", out var ru));
        Assert.Equal("+79123456789", ru);
    }

    [Fact]
    public void Purchase_request_rejects_endless_azerbaijani_digits()
    {
        var request = new FinalProject.MVC.Models.ShowroomPurchaseRequest
        {
            ProductId = 9,
            CustomerName = "Ali",
            Email = "ali@example.com",
            Phone = "+99456655151515151561511",
            Quantity = 1,
            SubmissionKey = Guid.NewGuid()
        };

        var error = ShowroomRequestRules.Validate(request, 7, DateTime.Today, "AZ");
        Assert.Equal(InternationalPhoneNumber.ErrorMessage, error);
    }
}
