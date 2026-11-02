using FinalProject.MVC.Security;
using Xunit;

namespace FinalProject.Tests;

public class AdminUserSearchTests
{
    [Fact]
    public void Empty_search_matches_everyone()
    {
        Assert.True(AdminUserSearch.Matches(null, "Ali", "Mammadli", "ali@example.com", "555"));
        Assert.True(AdminUserSearch.Matches("  ", "Ali", "Mammadli", "ali@example.com", "555"));
    }

    [Theory]
    [InlineData("ali", true)]
    [InlineData("MAM", true)]
    [InlineData("ali@example.com", true)]
    [InlineData("555-01", true)]
    [InlineData("zzz", false)]
    public void Search_matches_name_email_or_phone(string term, bool expected)
    {
        Assert.Equal(
            expected,
            AdminUserSearch.Matches(term, "Ali", "Mammadli", "ali@example.com", "555-0100"));
    }
}
