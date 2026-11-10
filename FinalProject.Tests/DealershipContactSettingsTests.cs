using FinalProject.MVC.Configuration;
using Xunit;

namespace FinalProject.Tests;

public class DealershipContactSettingsTests
{
    [Fact]
    public void Tel_and_whatsapp_hrefs_use_official_normalized_number()
    {
        var settings = new DealershipContactSettings();

        Assert.Equal("tel:+994503546767", settings.TelHref);
        Assert.Equal("https://wa.me/994503546767", settings.WhatsAppHref);
        Assert.Equal("mailto:barisking333@gmail.com", settings.MailtoHref);
        Assert.Equal("+994 50 354 67 67", settings.DisplayPhone);
        Assert.NotEqual(settings.TelHref, settings.WhatsAppHref);
        Assert.StartsWith("tel:", settings.TelHref);
        Assert.StartsWith("https://wa.me/", settings.WhatsAppHref);
        Assert.DoesNotContain("tel:", settings.WhatsAppHref);
        Assert.DoesNotContain("call=", settings.WhatsAppHref);
    }

    [Fact]
    public void NormalizeE164_accepts_spaced_display_number()
    {
        Assert.Equal("+994503546767", DealershipContactSettings.NormalizeE164("+994 50 354 67 67"));
        Assert.Equal("+994503546767", DealershipContactSettings.NormalizeE164("994503546767"));
    }

    [Fact]
    public void TelHrefFor_builds_dialable_link_for_catalog_numbers()
    {
        Assert.Equal("tel:+994503546767", DealershipContactSettings.TelHrefFor("+994503546767"));
        Assert.Equal("tel:+994503546767", DealershipContactSettings.TelHrefFor(DealershipContactDefaults.DisplayPhone));
    }
}
