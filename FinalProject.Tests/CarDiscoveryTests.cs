using FinalProject.Commerce;
using FinalProject.Data;
using FinalProject.Models.Entities;
using FinalProject.Services;
using FinalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FinalProject.Tests;

public class CarDiscoveryTests
{
    [Fact]
    public void Today_starts_at_local_midnight_in_Baku()
    {
        var utcNow = ToUtc(new DateTime(2026, 10, 25, 15, 0, 0));
        var start = CarDiscoveryRules.PeriodStartUtc(CarDiscoveryRules.Today, utcNow);

        Assert.Equal(TimeSpan.FromHours(4), CarDiscoveryRules.DealershipZone.GetUtcOffset(utcNow));
        Assert.Equal(ToUtc(new DateTime(2026, 10, 25, 0, 0, 0)), start);
        Assert.Null(CarDiscoveryRules.PeriodStartUtc(CarDiscoveryRules.All, utcNow));
        Assert.False(CarDiscoveryRules.TryNormalizePeriod("yesterday", out _));
    }

    [Fact]
    public void Last_seven_days_include_today_and_the_previous_six_local_days()
    {
        var utcNow = ToUtc(new DateTime(2026, 10, 25, 15, 0, 0));
        var start = CarDiscoveryRules.PeriodStartUtc(CarDiscoveryRules.Week, utcNow);

        Assert.Equal(ToUtc(new DateTime(2026, 10, 19, 0, 0, 0)), start);
    }

    [Fact]
    public async Task Every_details_open_is_counted_including_repeats()
    {
        await using var db = CreateDb();
        var car = await SeedCarAsync(db, "A", new DateTime(2026, 10, 1));
        var discovery = new CarDiscoveryService(db);
        var when = ToUtc(new DateTime(2026, 10, 25, 12, 0, 0));

        Assert.True(await discovery.RecordViewAsync(car.Id, when));
        Assert.True(await discovery.RecordViewAsync(car.Id, when));
        Assert.False(await discovery.RecordViewAsync(999));

        var ranked = await discovery.GetMostViewedAsync(CarDiscoveryRules.All, utcNow: when);
        Assert.Single(ranked.Items);
        Assert.Equal(2, ranked.Items[0].Count);
        Assert.Equal(2, await db.CarViews.CountAsync());
    }

    [Fact]
    public async Task Most_viewed_filters_by_period_and_sorts_by_count()
    {
        await using var db = CreateDb();
        var popular = await SeedCarAsync(db, "Popular", new DateTime(2026, 10, 1));
        var recent = await SeedCarAsync(db, "Recent", new DateTime(2026, 10, 20));
        var old = await SeedCarAsync(db, "Old", new DateTime(2026, 9, 1));
        var discovery = new CarDiscoveryService(db);
        var now = ToUtc(new DateTime(2026, 10, 25, 15, 0, 0));

        await discovery.RecordViewAsync(popular.Id, ToUtc(new DateTime(2026, 10, 25, 9, 0, 0)));
        await discovery.RecordViewAsync(popular.Id, ToUtc(new DateTime(2026, 10, 24, 9, 0, 0)));
        await discovery.RecordViewAsync(popular.Id, ToUtc(new DateTime(2026, 10, 20, 9, 0, 0)));
        await discovery.RecordViewAsync(recent.Id, ToUtc(new DateTime(2026, 10, 25, 10, 0, 0)));
        await discovery.RecordViewAsync(recent.Id, ToUtc(new DateTime(2026, 10, 25, 11, 0, 0)));
        await discovery.RecordViewAsync(old.Id, ToUtc(new DateTime(2026, 10, 1, 9, 0, 0)));

        var today = await discovery.GetMostViewedAsync(CarDiscoveryRules.Today, utcNow: now);
        Assert.Equal(new[] { recent.Id, popular.Id }, today.Items.Select(item => item.Car.Id).ToArray());
        Assert.Equal(2, today.Items[0].Count);
        Assert.Equal(1, today.Items[1].Count);

        var week = await discovery.GetMostViewedAsync(CarDiscoveryRules.Week, utcNow: now);
        Assert.Equal(new[] { popular.Id, recent.Id }, week.Items.Select(item => item.Car.Id).ToArray());
        Assert.DoesNotContain(week.Items, item => item.Car.Id == old.Id);

        var all = await discovery.GetMostViewedAsync(CarDiscoveryRules.All, utcNow: now);
        Assert.Equal(popular.Id, all.Items[0].Car.Id);
        Assert.Equal(3, all.Items[0].Count);
        Assert.Contains(all.Items, item => item.Car.Id == old.Id);
    }

    [Fact]
    public async Task Favorites_are_unique_per_account_and_rank_current_saves()
    {
        await using var db = CreateDb();
        var first = await SeedCarAsync(db, "First", new DateTime(2026, 10, 2));
        var second = await SeedCarAsync(db, "Second", new DateTime(2026, 10, 3));
        db.IdentityUsers.AddRange(
            new IdentityUserReference { Id = "user-1", UserName = "one" },
            new IdentityUserReference { Id = "user-2", UserName = "two" });
        await db.SaveChangesAsync();

        var marketplace = new MarketplaceService(db, new ConfigurationBuilder().Build(), null!);
        Assert.NotNull(await marketplace.AddFavoriteAsync("user-1", first.Id));
        Assert.NotNull(await marketplace.AddFavoriteAsync("user-1", first.Id));
        Assert.NotNull(await marketplace.AddFavoriteAsync("user-2", first.Id));
        Assert.NotNull(await marketplace.AddFavoriteAsync("user-2", second.Id));
        Assert.Equal(1, await db.Favorites.CountAsync(favorite => favorite.UserId == "user-1" && favorite.CarId == first.Id));

        var discovery = new CarDiscoveryService(db);
        var ranked = await discovery.GetMostFavoritedAsync();
        Assert.Equal(new[] { first.Id, second.Id }, ranked.Items.Select(item => item.Car.Id).ToArray());
        Assert.Equal(2, ranked.Items[0].Count);
        Assert.Equal(1, ranked.Items[1].Count);

        Assert.True(await marketplace.RemoveFavoriteAsync("user-2", first.Id));
        ranked = await discovery.GetMostFavoritedAsync();
        Assert.Equal(1, ranked.Items.Single(item => item.Car.Id == first.Id).Count);
    }

    [Fact]
    public async Task Latest_arrivals_use_listing_dates_newest_first()
    {
        await using var db = CreateDb();
        var older = await SeedCarAsync(db, "Older", new DateTime(2026, 1, 1, 8, 0, 0));
        var newer = await SeedCarAsync(db, "Newer", new DateTime(2026, 10, 20, 8, 0, 0));
        var middle = await SeedCarAsync(db, "Middle", new DateTime(2026, 5, 1, 8, 0, 0));

        var arrivals = await new CarDiscoveryService(db).GetLatestArrivalsAsync();

        Assert.Equal(new[] { newer.Id, middle.Id, older.Id }, arrivals.Items.Select(car => car.Id).ToArray());
    }

    [Fact]
    public async Task Explore_pages_return_nine_cars_per_page_with_correct_slice()
    {
        await using var db = CreateDb();
        var discovery = new CarDiscoveryService(db);
        var cars = new List<Car>();
        for (var i = 1; i <= 23; i++)
        {
            cars.Add(await SeedCarAsync(db, $"Car{i:00}", new DateTime(2026, 1, 1).AddDays(i)));
        }

        var viewedAt = ToUtc(new DateTime(2026, 10, 25, 10, 0, 0));
        for (var i = 0; i < cars.Count; i++)
        {
            for (var views = 0; views < cars.Count - i; views++)
            {
                db.CarViews.Add(new CarView { CarId = cars[i].Id, ViewedAt = viewedAt });
            }
        }
        await db.SaveChangesAsync();

        var page1 = await discovery.GetMostViewedAsync(CarDiscoveryRules.All, page: 1, pageSize: 9);
        var page2 = await discovery.GetMostViewedAsync(CarDiscoveryRules.All, page: 2, pageSize: 9);
        var page3 = await discovery.GetMostViewedAsync(CarDiscoveryRules.All, page: 3, pageSize: 9);

        Assert.Equal(23, page1.TotalCount);
        Assert.Equal(3, page1.TotalPages);
        Assert.Equal(9, page1.Items.Count);
        Assert.Equal(9, page2.Items.Count);
        Assert.Equal(5, page3.Items.Count);
        Assert.Equal(cars[0].Id, page1.Items[0].Car.Id);
        Assert.Equal(cars[9].Id, page2.Items[0].Car.Id);
        Assert.Equal(cars[18].Id, page3.Items[0].Car.Id);
        Assert.Empty(page1.Items.Select(item => item.Car.Id).Intersect(page2.Items.Select(item => item.Car.Id)));

        var latest1 = await discovery.GetLatestArrivalsAsync(page: 1, pageSize: 9);
        var latest3 = await discovery.GetLatestArrivalsAsync(page: 3, pageSize: 9);
        Assert.Equal(23, latest1.TotalCount);
        Assert.Equal(3, latest1.TotalPages);
        Assert.Equal(cars[^1].Id, latest1.Items[0].Id);
        Assert.Equal(5, latest3.Items.Count);

        Assert.Equal((1, 9), CarDiscoveryRules.NormalizePaging(0, 0));
        Assert.Equal((2, 48), CarDiscoveryRules.NormalizePaging(2, 100));
    }

    [Fact]
    public async Task Statistics_count_views_favorites_and_daily_trend()
    {
        await using var db = CreateDb();
        var car = await SeedCarAsync(db, "Trend", new DateTime(2026, 10, 1));
        var discovery = new CarDiscoveryService(db);
        var now = ToUtc(new DateTime(2026, 10, 25, 15, 0, 0));
        await discovery.RecordViewAsync(car.Id, ToUtc(new DateTime(2026, 10, 25, 9, 0, 0)));
        await discovery.RecordViewAsync(car.Id, ToUtc(new DateTime(2026, 10, 24, 9, 0, 0)));
        db.Favorites.Add(new Favorite { UserId = "user-1", CarId = car.Id, CreatedAt = now });
        db.IdentityUsers.Add(new IdentityUserReference { Id = "user-1" });
        await db.SaveChangesAsync();

        var stats = await discovery.GetStatisticsAsync(now);

        Assert.Equal(2, stats.TotalViews);
        Assert.Equal(1, stats.ViewsToday);
        Assert.Equal(2, stats.ViewsLast7Days);
        Assert.Equal(1, stats.TotalFavorites);
        Assert.Equal(14, stats.DailyViews.Count);
        Assert.Equal(1, stats.DailyViews.Single(day => day.Date == "2026-10-25").Views);
        Assert.Equal(1, stats.DailyViews.Single(day => day.Date == "2026-10-24").Views);
        Assert.Equal(0, stats.DailyViews.Single(day => day.Date == "2026-10-12").Views);
    }

    [Fact]
    public async Task Used_browse_returns_every_active_used_car_and_excludes_new_cars()
    {
        await using var db = CreateDb();
        var viewedUsed = await SeedCarAsync(db, "ViewedUsed", new DateTime(2026, 1, 2));
        var quietUsed = await SeedCarAsync(db, "QuietUsed", new DateTime(2026, 1, 1));
        var newCar = await SeedCarAsync(db, "NewCar", new DateTime(2026, 6, 1));
        var draftUsed = await SeedCarAsync(db, "DraftUsed", new DateTime(2026, 3, 1));
        newCar.IsNew = true;
        draftUsed.Status = "Draft";
        await db.SaveChangesAsync();

        var discovery = new CarDiscoveryService(db);
        await discovery.RecordViewAsync(viewedUsed.Id, new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        await discovery.RecordViewAsync(viewedUsed.Id, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc));
        await discovery.RecordViewAsync(newCar.Id, new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc));

        var used = await discovery.GetMostViewedAsync("all", page: 1, pageSize: 500, usedOnly: true);
        Assert.Equal(2, used.TotalCount);
        Assert.Equal(new[] { viewedUsed.Id, quietUsed.Id }, used.Items.Select(item => item.Car.Id).ToArray());
        Assert.Equal(2, used.Items[0].Count);
        Assert.Equal(0, used.Items[1].Count);
        Assert.All(used.Items, item => Assert.False(item.Car.IsNew));

        var ranked = await discovery.GetMostViewedAsync("all", page: 1, pageSize: 9);
        Assert.Contains(ranked.Items, item => item.Car.Id == newCar.Id);
        Assert.DoesNotContain(ranked.Items, item => item.Car.Id == quietUsed.Id);
    }

    [Fact]
    public async Task Used_browse_pages_nine_active_used_cars_and_keeps_later_pages()
    {
        await using var db = CreateDb();
        var discovery = new CarDiscoveryService(db);
        var usedIds = new List<int>();
        for (var index = 0; index < 10; index++)
        {
            var car = await SeedCarAsync(db, "Used" + index, new DateTime(2026, 1, index + 1));
            car.IsNew = false;
            usedIds.Add(car.Id);
        }

        var newCar = await SeedCarAsync(db, "BrandNew", new DateTime(2026, 8, 1));
        newCar.IsNew = true;
        await db.SaveChangesAsync();

        var first = await discovery.GetMostViewedAsync("all", page: 1, pageSize: 9, usedOnly: true);
        var second = await discovery.GetMostViewedAsync("all", page: 2, pageSize: 9, usedOnly: true);

        Assert.Equal(10, first.TotalCount);
        Assert.Equal(2, first.TotalPages);
        Assert.Equal(9, first.Items.Count);
        Assert.Single(second.Items);
        Assert.Equal(9, FinalProject.MVC.Models.CarExplorePage.UsedPageSize);
        Assert.All(first.Items.Concat(second.Items), item => Assert.False(item.Car.IsNew));
        Assert.DoesNotContain(first.Items.Concat(second.Items), item => item.Car.Id == newCar.Id);
        Assert.Equal(usedIds.Count, first.Items.Concat(second.Items).Select(item => item.Car.Id).Distinct().Count());
    }

    private static DateTime ToUtc(DateTime bakuLocal) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(bakuLocal, DateTimeKind.Unspecified), CarDiscoveryRules.DealershipZone);

    private static FinalProjectDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseInMemoryDatabase("car-discovery-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new FinalProjectDbContext(options);
    }

    private static async Task<Car> SeedCarAsync(FinalProjectDbContext db, string model, DateTime createdAt)
    {
        var brand = await db.Brands.FirstOrDefaultAsync() ?? db.Brands.Add(new Brand { Name = "Vault", CreatedAt = createdAt }).Entity;
        var category = await db.Categories.FirstOrDefaultAsync() ?? db.Categories.Add(new Category { Name = "Sedan" }).Entity;
        await db.SaveChangesAsync();
        var car = new Car
        {
            BrandId = brand.Id,
            CategoryId = category.Id,
            Model = model,
            Year = 2024,
            Price = 10000,
            Mileage = 1000,
            FuelType = "Petrol",
            Transmission = "Automatic",
            Color = "Black",
            Description = model,
            Status = "Active",
            CreatedAt = createdAt
        };
        db.Cars.Add(car);
        await db.SaveChangesAsync();
        return car;
    }
}
