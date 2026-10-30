using FinalProject.Commerce;
using Xunit;

namespace FinalProject.Tests;

public class CarGalleryRulesTests
{
    [Theory]
    [InlineData("/assets/images/catalog/bmw/m340i.jpg")]
    [InlineData("~/assets/images/catalog/convertible/mercedes-sl-class-black.jpg")]
    [InlineData("/assets/images/catalog/sports-car/lamborghini-diablo-red.jpg")]
    [InlineData("~/assets/images/portfolio/cars/uploads/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg")]
    [InlineData("/assets/images/portfolio/cars/uploads/bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.png")]
    [InlineData("~/assets/images/portfolio/cars/uploads/0826b533ccd2488b964340043ba9d951.jpg")]
    public void IsSafeReference_allows_existing_catalog_and_upload_paths(string path)
    {
        Assert.True(CarGalleryRules.IsSafeReference(path));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://cdn.example.com/car.jpg")]
    [InlineData("http://localhost/assets/images/catalog/x.jpg")]
    [InlineData("//evil.example/car.jpg")]
    [InlineData("~/assets/images/catalog/../secret.jpg")]
    [InlineData("~/assets/images/portfolio/cars/uploads/../secret.jpg")]
    [InlineData(@"~\assets\images\catalog\bmw.jpg")]
    [InlineData("~/assets/images/portfolio/01.jpg")]
    [InlineData("~/assets/images/blog/post.jpg")]
    [InlineData("/assets/images/parts/filter.webp")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/png;base64,abc")]
    public void IsSafeReference_rejects_external_and_unsafe_paths(string? path)
    {
        Assert.False(CarGalleryRules.IsSafeReference(path));
    }

    [Fact]
    public void IsSafeReference_rejects_protocol_relative_even_when_slash_prefixed()
    {
        Assert.False(CarGalleryRules.IsSafeReference("//cdn.example.com/assets/images/catalog/x.jpg"));
        Assert.True(CarGalleryRules.IsSafeReference("/assets/images/catalog/x.jpg"));
    }
}
