using FinalProject.MVC.Security;
using Xunit;

namespace FinalProject.Tests;

public class AccountHeaderTests
{
    [Theory]
    [InlineData("Ada", "Tester", "ada", "ada@example.com", "Ada Tester")]
    [InlineData(null, null, "ada", "ada@example.com", "ada")]
    [InlineData(" ", " ", " ", "ada@example.com", "ada@example.com")]
    [InlineData(null, null, null, null, "Account")]
    public void Display_name_uses_full_name_then_username_then_email(
        string? firstName,
        string? lastName,
        string? userName,
        string? email,
        string expected)
    {
        Assert.Equal(expected, AccountDisplayName.Resolve(firstName, lastName, userName, email));
    }

    [Fact]
    public void Logged_out_header_shows_register_beside_sign_in_and_keeps_the_dealership_phone()
    {
        var layout = Read("FinalProject.MVC", "Views", "Shared", "_Layout.cshtml");
        var buttons = Read("FinalProject.MVC", "Views", "Shared", "_AccountHeaderButtons.cshtml");
        var users = Read("FinalProject.MVC", "Views", "Admin", "Users.cshtml");
        var details = Read("FinalProject.MVC", "Views", "Admin", "UserDetails.cshtml");
        var profile = Read("FinalProject.MVC", "Views", "Account", "Profile.cshtml");
        var settings = Read("FinalProject.MVC", "Views", "AdminSettings", "Index.cshtml");

        Assert.Contains("asp-action=\"SignIn\"", buttons, StringComparison.Ordinal);
        Assert.Contains("asp-action=\"Register\"", buttons, StringComparison.Ordinal);
        Assert.Contains("asp-action=\"Index\"", buttons, StringComparison.Ordinal);
        Assert.Contains("_AccountHeaderButtons", layout, StringComparison.Ordinal);
        Assert.Contains("Site.Contact.DisplayPhone", layout, StringComparison.Ordinal);
        Assert.Contains("asp-for=\"DisplayPhone\"", settings, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-for=\"PhoneNumber\"", profile, StringComparison.Ordinal);
        Assert.DoesNotContain(">Phone status<", users, StringComparison.Ordinal);
        Assert.DoesNotContain(">Phone<", details, StringComparison.Ordinal);
    }

    private static string Read(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FinalProject.slnx")))
        {
            dir = dir.Parent;
        }

        var root = dir?.FullName ?? throw new InvalidOperationException("Could not find the solution root.");
        return File.ReadAllText(Path.Combine(new[] { root }.Concat(parts).ToArray()));
    }
}
