using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models;
using Xunit;

namespace FinalProject.Tests;

public class ContactFormValidationTests
{
    [Theory]
    [InlineData("+994 50 354 67 67")]
    [InlineData("+1 (555) 123-4567")]
    [InlineData("050-354-6767")]
    [InlineData("994503546767")]
    public void PhoneNumberValidation_AcceptsFormattedNumbers(string phone)
    {
        Assert.True(PhoneNumberValidation.IsValid(phone));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("123abc")]
    [InlineData("hello123")]
    [InlineData("1")]
    [InlineData("12")]
    [InlineData("++994503546767")]
    [InlineData("994+503546767")]
    public void PhoneNumberValidation_RejectsInvalidValues(string? phone)
    {
        Assert.False(PhoneNumberValidation.IsValid(phone));
    }

    [Fact]
    public void ContactFormRules_RejectsLettersInPhoneWithoutStripping()
    {
        var model = ValidModel();
        model.Phone = "1234567abc";

        var errors = ContactFormRules.Validate(model);

        Assert.True(errors.ContainsKey("phone"));
        Assert.Equal(InternationalPhoneNumber.ErrorMessage, errors["phone"]);
        Assert.Equal("1234567abc", model.Phone);
    }

    [Fact]
    public void ContactFormRules_AcceptsValidSubmission()
    {
        var errors = ContactFormRules.Validate(ValidModel());

        Assert.Empty(errors);
    }

    [Fact]
    public void ContactFormRules_RejectsEmptyNameEmailMessage()
    {
        var model = new ContactFormModel
        {
            Name = " ",
            Email = "not-an-email",
            Phone = "abc",
            Car = "",
            Message = "short"
        };

        var errors = ContactFormRules.Validate(model);

        Assert.Contains("name", errors.Keys);
        Assert.Contains("email", errors.Keys);
        Assert.Contains("phone", errors.Keys);
        Assert.Contains("car", errors.Keys);
        Assert.Contains("message", errors.Keys);
    }

    [Theory]
    [InlineData("AZ", "501234567", "+994501234567")]
    [InlineData("TR", "5321234567", "+905321234567")]
    [InlineData("US", "5551234567", "+15551234567")]
    [InlineData("GB", "7400123456", "+447400123456")]
    public void ContactFormRules_stores_the_complete_international_number(string country, string national, string expected)
    {
        var model = ValidModel();
        model.PhoneCountry = country;
        model.Phone = national;

        var errors = ContactFormRules.Validate(model);

        Assert.Empty(errors);
        Assert.Equal(expected, model.Phone);
    }

    private static ContactFormModel ValidModel() => new()
    {
        Name = "Ada Lovelace",
        Email = "ada@example.com",
        Phone = "+994 50 354 67 67",
        Car = "Brake pads",
        Message = "Please call me about availability this week."
    };
}
