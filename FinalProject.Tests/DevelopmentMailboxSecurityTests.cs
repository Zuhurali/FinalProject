using FinalProject.MVC.Services;
using Xunit;

namespace FinalProject.Tests;

public class DevelopmentMailboxSecurityTests
{
    [Fact]
    public void Confirmation_and_reset_tokens_are_hidden_while_the_link_stays_usable()
    {
        const string body =
            "Confirm Email: https://localhost:7245/Account/ConfirmEmail?userId=abc&code=SECRET-TOKEN\n" +
            "Reset: https://localhost:7245/Account/ResetPassword?code=RESET-TOKEN\n" +
            "Store: https://localhost:7245/Contact";

        var display = DevelopmentMailDisplay.RedactSensitiveLinks(body);
        Assert.DoesNotContain("SECRET-TOKEN", display, StringComparison.Ordinal);
        Assert.DoesNotContain("RESET-TOKEN", display, StringComparison.Ordinal);
        Assert.Contains("[secure link hidden]", display, StringComparison.Ordinal);
        Assert.Contains("https://localhost:7245/Contact", display, StringComparison.Ordinal);

        var links = DevelopmentMailDisplay.SensitiveLinks(body);
        Assert.Equal(2, links.Count);
        Assert.Contains("SECRET-TOKEN", links[0], StringComparison.Ordinal);
        Assert.True(DevelopmentMailDisplay.CanFollow(links[0]));
        Assert.True(DevelopmentMailDisplay.CanFollow("https://autovault.example/Account/ConfirmEmail?token=abc"));
        Assert.False(DevelopmentMailDisplay.CanFollow("javascript:alert(1)"));
    }

    [Fact]
    public void Mailbox_page_requires_development_admin_and_does_not_render_raw_html()
    {
        var root = FindRepositoryRoot();
        var controller = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "DevMailboxController.cs"));
        var view = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "DevMailbox", "Message.cshtml"));

        Assert.Contains("IsDevelopment()", controller, StringComparison.Ordinal);
        Assert.Contains("IsInRole(\"Admin\")", controller, StringComparison.Ordinal);
        Assert.Contains("IsInRole(\"SuperAdmin\")", controller, StringComparison.Ordinal);
        Assert.Contains("OpenLink", controller, StringComparison.Ordinal);
        Assert.Contains("ValidateAntiForgeryToken", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("Html.Raw", view, StringComparison.Ordinal);
        Assert.Contains("DisplayBody", view, StringComparison.Ordinal);
        Assert.Contains("Open secure link", view, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FinalProject.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Could not find FinalProject.slnx.");
    }
}
