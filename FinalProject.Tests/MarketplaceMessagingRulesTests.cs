using FinalProject.Commerce;
using Xunit;

namespace FinalProject.Tests;

public class MarketplaceMessagingRulesTests
{
    [Fact]
    public void StripGeneratedListingText_removes_listing_line_and_keeps_buyer_text()
    {
        var result = MarketplaceMessagingRules.StripGeneratedListingText(
            "Hi, I'm interested in the BMW X5.\n\nListing: /Cars/Details/12\n\nI'd like to ask about pricing.");

        Assert.DoesNotContain("/Cars/Details/12", result);
        Assert.DoesNotContain("Listing:", result);
        Assert.Contains("BMW X5", result);
        Assert.Contains("I'd like to ask about pricing.", result);
    }

    [Fact]
    public void StripGeneratedListingText_removes_generated_footer_path()
    {
        var result = MarketplaceMessagingRules.StripGeneratedListingText(
            "Can you discount this?\n\n---\nRegarding: BMW X5\n/Cars/Details/11");

        Assert.Equal("Can you discount this?", result);
        Assert.DoesNotContain("/Cars/Details/11", result);
    }

    [Fact]
    public void DefaultSubject_includes_car_name()
    {
        Assert.Equal("Inquiry about Audi A4", MarketplaceMessagingRules.DefaultSubject("Audi A4"));
    }
}
