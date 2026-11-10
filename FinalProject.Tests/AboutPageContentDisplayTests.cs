using FinalProject.MVC.Commerce;
using FinalProject.MVC.Services;
using Xunit;

namespace FinalProject.Tests;

public class AboutPageContentDisplayTests
{
    [Fact]
    public void DisplayText_replaces_upload_token_leak_in_eyebrow()
    {
        var leaked = "About 78df205c01a24fd993e13cbab853d1aa";
        Assert.Equal(
            AboutPageContentDefaults.Eyebrow,
            AboutPageContentService.DisplayText(leaked, AboutPageContentDefaults.Eyebrow));
        Assert.Equal(
            AboutPageContentDefaults.Eyebrow,
            AboutPageContentService.DisplayText("78df205c01a24fd993e13cbab853d1aa", AboutPageContentDefaults.Eyebrow));
        Assert.Equal(
            "About Us",
            AboutPageContentService.DisplayText("About Us", AboutPageContentDefaults.Eyebrow));
    }
}
