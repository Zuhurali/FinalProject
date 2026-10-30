using FinalProject.MVC.Commerce;
using FinalProject.MVC.Mappings;
using FinalProject.MVC.Models;
using FinalProject.MVC.Models.Api;
using Xunit;

namespace FinalProject.Tests;

public class HomeCarShowcaseTests
{
    [Fact]
    public void Filters_split_new_used_trending_and_in_stock_by_real_fields()
    {
        var cars = new[]
        {
            Car(1, isNew: true, isFeatured: false, status: "Active"),
            Car(2, isNew: true, isFeatured: true, status: "Active"),
            Car(3, isNew: false, isFeatured: true, status: "Sold"),
            Car(4, isNew: false, isFeatured: false, status: "Active"),
            Car(5, isNew: false, isFeatured: false, status: "Draft")
        };

        Assert.Equal(new[] { 1, 2 }, HomeCarShowcase.NewCars(cars).Select(car => car.Id));
        Assert.Equal(new[] { 3, 4, 5 }, HomeCarShowcase.UsedCars(cars).Select(car => car.Id));
        Assert.Empty(HomeCarShowcase.NewCars(cars).Select(car => car.Id).Intersect(HomeCarShowcase.UsedCars(cars).Select(car => car.Id)));
        Assert.Equal(new[] { 1, 2, 4 }, HomeCarShowcase.InStockCars(cars).Select(car => car.Id));
        Assert.All(HomeCarShowcase.InStockCars(cars), car => Assert.Equal("Active", car.Status));
    }

    [Fact]
    public void Trending_uses_newest_year_then_name_and_keeps_six()
    {
        var cars = Enumerable.Range(1, 8)
            .Select(id => Car(id, isNew: id % 2 == 0, isFeatured: false, status: "Active", year: 2010 + id, name: $"Model {id:00}"))
            .ToArray();

        var trending = HomeCarShowcase.TrendingCars(cars);

        Assert.Equal(6, trending.Count);
        Assert.Equal(new[] { 2018, 2017, 2016, 2015, 2014, 2013 }, trending.Select(car => car.Year));
        Assert.DoesNotContain(trending, car => car.Id == 1);
    }

    [Fact]
    public void Premium_prefers_mercedes_then_newest_year_without_replacing_the_section()
    {
        var cars = new[]
        {
            Car(1, true, false, "Active", 2025, "Audi", "A8"),
            Car(2, false, false, "Active", 2021, "Mercedes-Benz", "S-Class"),
            Car(3, true, false, "Active", 2023, "Mercedes-Benz", "Maybach"),
            Car(4, false, false, "Sold", 2024, "BMW", "M5"),
            Car(5, false, false, "Active", 2019, "Mercedes-Benz", "AMG GT"),
            Car(6, true, false, "Active", 2022, "Mercedes-Benz", "EQS"),
            Car(7, false, false, "Active", 2020, "Mercedes-Benz", "E-Class"),
            Car(8, false, false, "Active", 2018, "Mercedes-Benz", "SL-Class")
        };

        var premium = HomeCarShowcase.PremiumCars(cars);

        Assert.Equal(6, premium.Count);
        Assert.Equal(new[] { 3, 6, 2, 7, 5, 8 }, premium.Select(car => car.Id));
        Assert.All(premium, car => Assert.Contains("Mercedes", car.Make, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(premium, car => car.Id == 1);
    }

    [Fact]
    public void Premium_shows_only_IsFeatured_cars_when_any_are_marked()
    {
        var cars = new[]
        {
            Car(1, true, isFeatured: true, "Active", 2018, "Audi", "A4"),
            Car(2, false, isFeatured: false, "Active", 2025, "Mercedes-Benz", "S-Class"),
            Car(3, true, isFeatured: false, "Active", 2024, "BMW", "M5"),
            Car(4, false, isFeatured: true, "Active", 2020, "Nissan", "350Z")
        };

        var premium = HomeCarShowcase.PremiumCars(cars);

        Assert.Equal(new[] { 4, 1 }, premium.Select(car => car.Id));
        Assert.All(premium, car => Assert.True(car.IsFeatured));
        Assert.DoesNotContain(premium, car => car.Id is 2 or 3);
    }

    [Fact]
    public void Premium_falls_back_to_mercedes_heuristic_when_no_car_is_featured()
    {
        var cars = new[]
        {
            Car(1, true, false, "Active", 2025, "Audi", "A8"),
            Car(2, false, false, "Active", 2021, "Mercedes-Benz", "S-Class")
        };

        var premium = HomeCarShowcase.PremiumCars(cars);

        Assert.Equal(2, premium[0].Id);
        Assert.All(premium, car => Assert.False(car.IsFeatured));
    }

    [Fact]
    public void Showcase_and_premium_include_admin_uploaded_car_images()
    {
        var featuredUpload = Car(9, false, isFeatured: true, "Active", 2019, "Toyota", "Upload Featured");
        featuredUpload.ImagePath = "~/assets/images/portfolio/cars/uploads/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.webp";
        var mercedes = Car(2, false, isFeatured: false, "Active", 2025, "Mercedes-Benz", "S-Class");

        Assert.Contains(featuredUpload, HomeCarShowcase.ShowcaseCars(new[] { featuredUpload, mercedes }));
        var premium = HomeCarShowcase.PremiumCars(new[] { featuredUpload, mercedes });
        Assert.Equal(new[] { 9 }, premium.Select(car => car.Id));
    }

    [Theory]
    [InlineData("~/assets/images/catalog/bmw/m340i.jpg", true)]
    [InlineData("/assets/images/catalog/convertible/mercedes-sl-class-black.jpg", true)]
    [InlineData("~/assets/images/portfolio/cars/uploads/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg", true)]
    [InlineData("/assets/images/portfolio/cars/uploads/bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.png", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("https://cdn.example.com/car.jpg", false)]
    [InlineData("http://localhost/assets/images/catalog/x.jpg", false)]
    [InlineData("~/assets/images/catalog/../secret.jpg", false)]
    [InlineData("~/assets/images/portfolio/cars/uploads/../secret.jpg", false)]
    [InlineData("~/assets/images/portfolio/01.jpg", false)]
    [InlineData("~/assets/images/blog/post.jpg", false)]
    [InlineData(@"~\assets\images\catalog\bmw.jpg", false)]
    public void Showcase_image_path_allows_catalog_and_uploads_only(string path, bool expected)
    {
        Assert.Equal(expected, HomeCarShowcase.IsShowcaseImagePath(path));
    }

    [Fact]
    public void ShowcaseCars_keeps_catalog_and_upload_mains_and_drops_unsafe_paths()
    {
        var catalog = Car(1, true, false, "Active");
        catalog.ImagePath = "/assets/images/catalog/bmw/m340i.jpg";
        var upload = Car(2, false, false, "Active");
        upload.ImagePath = "~/assets/images/portfolio/cars/uploads/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg";
        var external = Car(3, false, false, "Active");
        external.ImagePath = "https://evil.example/car.jpg";
        var traversal = Car(4, false, false, "Active");
        traversal.ImagePath = "~/assets/images/catalog/../etc/passwd";
        var empty = Car(5, false, false, "Active");
        empty.ImagePath = "";

        var showcase = HomeCarShowcase.ShowcaseCars(new[] { catalog, upload, external, traversal, empty });

        Assert.Equal(new[] { 1, 2 }, showcase.Select(car => car.Id));
    }

    [Fact]
    public void PremiumCars_excludes_external_and_unsafe_image_paths()
    {
        var featuredExternal = Car(1, false, isFeatured: true, "Active", 2024, "Audi", "A8");
        featuredExternal.ImagePath = "https://cdn.example.com/featured.jpg";
        var mercedes = Car(2, false, isFeatured: false, "Active", 2021, "Mercedes-Benz", "S-Class");

        var premium = HomeCarShowcase.PremiumCars(new[] { featuredExternal, mercedes });

        Assert.Equal(new[] { 2 }, premium.Select(car => car.Id));
        Assert.DoesNotContain(premium, car => car.Id == 1);
    }

    [Fact]
    public void In_stock_requires_Active_status_not_merely_being_listed()
    {
        var sold = Car(20, isNew: true, isFeatured: true, status: "Sold");
        var draft = Car(21, isNew: false, isFeatured: false, status: "Draft");
        var active = Car(22, isNew: false, isFeatured: false, status: "Active");

        Assert.False(HomeCarShowcase.IsInStock(sold));
        Assert.False(HomeCarShowcase.IsInStock(draft));
        Assert.True(HomeCarShowcase.IsInStock(active));
        Assert.Empty(HomeCarShowcase.InStockCars(new[] { sold, draft }));
        Assert.Equal(new[] { 22 }, HomeCarShowcase.InStockCars(new[] { sold, draft, active }).Select(car => car.Id));
    }

    [Theory]
    [InlineData(true, "Active", "NEW", "IN STOCK")]
    [InlineData(false, "Active", "USED", "IN STOCK")]
    [InlineData(true, "Sold", "NEW", "OUT OF STOCK")]
    [InlineData(false, "Draft", "USED", "OUT OF STOCK")]
    public void Badges_reflect_condition_and_stock_without_contradiction(
        bool isNew,
        string status,
        string expectedCondition,
        string expectedStock)
    {
        var car = Car(1, isNew, isFeatured: false, status);

        Assert.Equal(expectedCondition, HomeCarShowcase.ConditionBadge(car));
        Assert.Equal(expectedStock, HomeCarShowcase.StockBadge(car));
        Assert.NotEqual(HomeCarShowcase.ConditionBadge(car), HomeCarShowcase.StockBadge(car));
        Assert.DoesNotContain("NEW", HomeCarShowcase.StockBadge(car));
        Assert.DoesNotContain("USED", HomeCarShowcase.StockBadge(car));
    }

    [Fact]
    public void Mapper_exposes_IsFeatured_and_derives_InStock_from_Status()
    {
        var dto = new CarApiDto
        {
            Id = 7,
            Model = "M3",
            IsNew = false,
            IsFeatured = true,
            Status = "Active",
            Brand = new BrandApiDto { Name = "BMW" },
            Images =
            {
                new CarImageApiDto
                {
                    ImageUrl = "/assets/images/catalog/bmw.webp",
                    IsMain = true,
                    DisplayOrder = 0
                }
            }
        };

        var car = CarApiMapper.ToCar(dto);

        Assert.True(car.IsFeatured);
        Assert.False(car.IsNew);
        Assert.Equal("Used", car.Badge);
        Assert.True(car.InStock);
        Assert.Equal("USED", HomeCarShowcase.ConditionBadge(car));
        Assert.Equal("IN STOCK", HomeCarShowcase.StockBadge(car));
        Assert.Contains(car, HomeCarShowcase.TrendingCars(new[] { car }));
        Assert.Contains(car, HomeCarShowcase.ShowcaseCars(new[] { car }));
    }

    [Fact]
    public void Home_used_carousel_selects_at_most_six_distinct_active_used_cars()
    {
        var cars = new List<Car>
        {
            Car(1, isNew: true, isFeatured: false, status: "Active", name: "New active"),
            Car(2, isNew: false, isFeatured: false, status: "Sold", name: "Used sold"),
            Car(3, isNew: false, isFeatured: false, status: "Draft", name: "Used draft")
        };
        for (var id = 10; id < 20; id++)
        {
            cars.Add(Car(id, isNew: false, isFeatured: false, status: "Active", name: $"Used {id}"));
            cars.Add(Car(id, isNew: false, isFeatured: false, status: "Active", name: $"Used duplicate {id}"));
        }

        var selected = HomeCarShowcase.HomeUsedCarousel(cars, new Random(7));

        Assert.Equal(6, selected.Count);
        Assert.Equal(6, selected.Select(car => car.Id).Distinct().Count());
        Assert.All(selected, car =>
        {
            Assert.False(car.IsNew);
            Assert.Equal("Active", car.Status);
            Assert.Equal("USED", HomeCarShowcase.ConditionBadge(car));
            Assert.Equal("IN STOCK", HomeCarShowcase.StockBadge(car));
        });
        Assert.DoesNotContain(selected, car => car.Id is 1 or 2 or 3);
    }

    [Fact]
    public void Home_used_carousel_returns_every_eligible_car_when_fewer_than_six()
    {
        var cars = new[]
        {
            Car(1, false, false, "Active"),
            Car(2, false, false, "Active"),
            Car(3, true, false, "Active"),
            Car(4, false, false, "Sold")
        };

        var selected = HomeCarShowcase.HomeUsedCarousel(cars, new Random(1));

        Assert.Equal(new[] { 1, 2 }, selected.Select(car => car.Id).OrderBy(id => id));
    }

    [Fact]
    public void Empty_filters_stay_empty_instead_of_falling_back_to_unrelated_cars()
    {
        var usedOnly = new[]
        {
            Car(1, isNew: false, isFeatured: false, status: "Active"),
            Car(2, isNew: false, isFeatured: false, status: "Sold")
        };

        Assert.Empty(HomeCarShowcase.NewCars(usedOnly));
        Assert.Equal(2, HomeCarShowcase.UsedCars(usedOnly).Count);
        Assert.Single(HomeCarShowcase.InStockCars(usedOnly));
    }

    private static Car Car(
        int id,
        bool isNew,
        bool isFeatured,
        string status,
        int year = 2020,
        string make = "BMW",
        string? name = null) => new()
    {
        Id = id,
        Name = name ?? $"Car {id}",
        Make = make,
        IsNew = isNew,
        IsFeatured = isFeatured,
        Status = status,
        InStock = string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase),
        Year = year,
        CreatedAt = DateTime.UtcNow.AddMinutes(-id),
        ImagePath = $"~/assets/images/catalog/{id}.webp"
    };
}
