using FinalProject.Commerce;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.Models.Entities;
using FinalProject.Services;
using FinalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests;

public class CarListQueryFilterSortTests
{
    [Fact]
    public void Validate_rejects_invalid_mileage_seats_and_sort()
    {
        Assert.Contains("Mileage", new CarListQueryDto { MinMileage = -1 }.Validate()!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("minMileage", new CarListQueryDto { MinMileage = 10, MaxMileage = 5 }.Validate()!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("seats", new CarListQueryDto { Seats = 0 }.Validate()!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("seats", new CarListQueryDto { Seats = 13 }.Validate()!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sort", new CarListQueryDto { Sort = "popular" }.Validate()!, StringComparison.OrdinalIgnoreCase);
        Assert.Null(new CarListQueryDto { MinMileage = 0, MaxMileage = 100, Seats = 5, Sort = "latest" }.Validate());
        Assert.Null(new CarListQueryDto().Validate());
    }

    [Fact]
    public async Task Min_and_Max_Mileage_are_inclusive()
    {
        await using var db = CreateDb();
        var low = await SeedAsync(db, "Low", mileage: 1000);
        var mid = await SeedAsync(db, "Mid", mileage: 5000);
        var high = await SeedAsync(db, "High", mileage: 9000);
        var service = new CarService(db, new SilentNotifications());

        var page = await service.QueryAsync(new CarListQueryDto
        {
            MinMileage = 5000,
            MaxMileage = 9000,
            Page = 1,
            PageSize = 50
        });

        Assert.Equal(2, page.Items.Count);
        Assert.Contains(page.Items, item => item.Id == mid.Id);
        Assert.Contains(page.Items, item => item.Id == high.Id);
        Assert.DoesNotContain(page.Items, item => item.Id == low.Id);
    }

    [Fact]
    public async Task Seats_matches_exact_database_value_only()
    {
        await using var db = CreateDb();
        var five = await SeedAsync(db, "Five", seats: 5);
        await SeedAsync(db, "Seven", seats: 7);
        await SeedAsync(db, "NullSeats", seats: null, description: "Family car with 5 seats listed in text");
        var service = new CarService(db, new SilentNotifications());

        var page = await service.QueryAsync(new CarListQueryDto
        {
            Seats = 5,
            Page = 1,
            PageSize = 50
        });

        Assert.Single(page.Items);
        Assert.Equal(five.Id, page.Items[0].Id);
    }

    [Theory]
    [InlineData("latest")]
    [InlineData("price-asc")]
    [InlineData("price-desc")]
    [InlineData("year-desc")]
    [InlineData("year-asc")]
    public async Task Sort_modes_order_results_with_stable_id_tiebreak(string sort)
    {
        await using var db = CreateDb();
        var olderCheap = await SeedAsync(db, "A", price: 10000, year: 2018, createdAt: DateTime.UtcNow.AddDays(-3));
        var newerMid = await SeedAsync(db, "B", price: 20000, year: 2020, createdAt: DateTime.UtcNow.AddDays(-1));
        var newestExpensive = await SeedAsync(db, "C", price: 30000, year: 2022, createdAt: DateTime.UtcNow);
        var service = new CarService(db, new SilentNotifications());

        var page = await service.QueryAsync(new CarListQueryDto
        {
            Sort = sort,
            Page = 1,
            PageSize = 50
        });

        var ids = page.Items.Select(item => item.Id).ToList();
        Assert.Equal(3, ids.Count);
        Assert.Equal(
            sort switch
            {
                "latest" => new[] { newestExpensive.Id, newerMid.Id, olderCheap.Id },
                "price-asc" => new[] { olderCheap.Id, newerMid.Id, newestExpensive.Id },
                "price-desc" => new[] { newestExpensive.Id, newerMid.Id, olderCheap.Id },
                "year-desc" => new[] { newestExpensive.Id, newerMid.Id, olderCheap.Id },
                "year-asc" => new[] { olderCheap.Id, newerMid.Id, newestExpensive.Id },
                _ => Array.Empty<int>()
            },
            ids);
    }

    [Fact]
    public async Task Omitting_Sort_keeps_legacy_brand_model_year_order()
    {
        await using var db = CreateDb();
        var brandB = db.Brands.Add(new Brand { Name = "Beta", CreatedAt = DateTime.UtcNow }).Entity;
        var brandA = db.Brands.Add(new Brand { Name = "Alpha", CreatedAt = DateTime.UtcNow }).Entity;
        var category = db.Categories.Add(new Category { Name = "Sedan" }).Entity;
        await db.SaveChangesAsync();

        var betaCar = await SeedAsync(db, "Zeta", brandId: brandB.Id, categoryId: category.Id, year: 2024);
        var alphaOlder = await SeedAsync(db, "Model", brandId: brandA.Id, categoryId: category.Id, year: 2020);
        var alphaNewer = await SeedAsync(db, "Model", brandId: brandA.Id, categoryId: category.Id, year: 2024);
        var service = new CarService(db, new SilentNotifications());

        var page = await service.QueryAsync(new CarListQueryDto { Page = 1, PageSize = 50 });
        Assert.Equal(new[] { alphaNewer.Id, alphaOlder.Id, betaCar.Id }, page.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task Combined_filters_and_Active_status_exclude_non_matching_and_non_active()
    {
        await using var db = CreateDb();
        var brand = await EnsureBrandAsync(db, "Toyota");
        var category = await EnsureCategoryAsync(db, "SUV");
        var match = await SeedAsync(
            db,
            "RAV4",
            brandId: brand.Id,
            categoryId: category.Id,
            price: 25000,
            year: 2021,
            mileage: 40000,
            seats: 5,
            fuel: "Hybrid",
            transmission: "Automatic",
            status: "Active");
        await SeedAsync(
            db,
            "RAV4",
            brandId: brand.Id,
            categoryId: category.Id,
            price: 25000,
            year: 2021,
            mileage: 40000,
            seats: 5,
            fuel: "Hybrid",
            transmission: "Automatic",
            status: "Draft");
        await SeedAsync(
            db,
            "Camry",
            brandId: brand.Id,
            categoryId: category.Id,
            price: 18000,
            year: 2019,
            mileage: 80000,
            seats: 5,
            fuel: "Petrol",
            transmission: "Automatic",
            status: "Active");
        var service = new CarService(db, new SilentNotifications());

        var page = await service.QueryAsync(new CarListQueryDto
        {
            BrandId = brand.Id,
            CategoryId = category.Id,
            Model = "RAV4",
            MinPrice = 20000,
            MaxPrice = 30000,
            MinYear = 2020,
            MaxYear = 2022,
            MinMileage = 30000,
            MaxMileage = 50000,
            Seats = 5,
            Fuel = "Hybrid",
            Transmission = "Automatic",
            Status = CarPublicListing.ActiveStatus,
            Sort = "price-asc",
            Page = 1,
            PageSize = 9
        });

        Assert.Single(page.Items);
        Assert.Equal(match.Id, page.Items[0].Id);
        Assert.Equal("Active", page.Items[0].Status, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Pagination_is_stable_across_pages_with_no_duplicate_ids()
    {
        await using var db = CreateDb();
        for (var i = 0; i < 20; i++)
        {
            await SeedAsync(
                db,
                $"Car{i:D2}",
                price: 10000 + i,
                createdAt: DateTime.UtcNow.AddMinutes(-i));
        }

        var service = new CarService(db, new SilentNotifications());
        var page1 = await service.QueryAsync(new CarListQueryDto
        {
            Status = CarPublicListing.ActiveStatus,
            Sort = "latest",
            Page = 1,
            PageSize = 9
        });
        var page2 = await service.QueryAsync(new CarListQueryDto
        {
            Status = CarPublicListing.ActiveStatus,
            Sort = "latest",
            Page = 2,
            PageSize = 9
        });
        var page3 = await service.QueryAsync(new CarListQueryDto
        {
            Status = CarPublicListing.ActiveStatus,
            Sort = "latest",
            Page = 3,
            PageSize = 9
        });

        Assert.Equal(20, page1.TotalCount);
        Assert.Equal(3, page1.TotalPages);
        Assert.Equal(9, page1.Items.Count);
        Assert.Equal(9, page2.Items.Count);
        Assert.Equal(2, page3.Items.Count);

        var allIds = page1.Items.Concat(page2.Items).Concat(page3.Items).Select(item => item.Id).ToList();
        Assert.Equal(20, allIds.Count);
        Assert.Equal(20, allIds.Distinct().Count());
    }

    [Fact]
    public async Task Search_model_fuel_and_transmission_filters_still_apply()
    {
        await using var db = CreateDb();
        var hit = await SeedAsync(db, "Corolla", fuel: "Petrol", transmission: "Manual", description: "Reliable daily driver");
        await SeedAsync(db, "Corolla", fuel: "Diesel", transmission: "Manual", description: "Other fuel");
        await SeedAsync(db, "Yaris", fuel: "Petrol", transmission: "Automatic", description: "Reliable hatch");
        var service = new CarService(db, new SilentNotifications());

        var page = await service.QueryAsync(new CarListQueryDto
        {
            Search = "Reliable",
            Model = "Corolla",
            Fuel = "Petrol",
            Transmission = "Manual",
            Page = 1,
            PageSize = 50
        });

        Assert.Single(page.Items);
        Assert.Equal(hit.Id, page.Items[0].Id);
    }

    private static FinalProjectDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseInMemoryDatabase("car-list-query-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new FinalProjectDbContext(options);
    }

    private static async Task<Brand> EnsureBrandAsync(FinalProjectDbContext db, string name)
    {
        var existing = await db.Brands.FirstOrDefaultAsync(brand => brand.Name == name);
        if (existing != null)
        {
            return existing;
        }

        var brand = db.Brands.Add(new Brand { Name = name, CreatedAt = DateTime.UtcNow }).Entity;
        await db.SaveChangesAsync();
        return brand;
    }

    private static async Task<Category> EnsureCategoryAsync(FinalProjectDbContext db, string name)
    {
        var existing = await db.Categories.FirstOrDefaultAsync(category => category.Name == name);
        if (existing != null)
        {
            return existing;
        }

        var category = db.Categories.Add(new Category { Name = name }).Entity;
        await db.SaveChangesAsync();
        return category;
    }

    private static async Task<Car> SeedAsync(
        FinalProjectDbContext db,
        string model,
        int? brandId = null,
        int? categoryId = null,
        decimal price = 15000,
        int year = 2020,
        int mileage = 25000,
        int? seats = 5,
        string fuel = "Petrol",
        string transmission = "Automatic",
        string status = "Active",
        string? description = null,
        DateTime? createdAt = null)
    {
        if (brandId == null)
        {
            brandId = (await EnsureBrandAsync(db, "Vault")).Id;
        }

        if (categoryId == null)
        {
            categoryId = (await EnsureCategoryAsync(db, "Sedan")).Id;
        }

        var car = new Car
        {
            BrandId = brandId.Value,
            CategoryId = categoryId.Value,
            Model = model,
            Year = year,
            Price = price,
            Mileage = mileage,
            Seats = seats,
            FuelType = fuel,
            Transmission = transmission,
            Color = "Black",
            Description = description ?? model,
            Status = status,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
        db.Cars.Add(car);
        await db.SaveChangesAsync();
        return car;
    }

    private sealed class SilentNotifications : INotificationService
    {
        public Task<NotificationListResponseDto> GetForUserAsync(string userId, int take = 50, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NotificationUnreadCountsResponseDto> GetUnreadCountsAsync(string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NotificationResponseDto?> CreateMessageNotificationAsync(
            string receiverId,
            string senderDisplayName,
            int conversationId,
            int messageId,
            string preview,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<NotificationResponseDto>> CreatePriceDropNotificationsAsync(
            int carId,
            string carName,
            decimal previousPrice,
            decimal newPrice,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NotificationResponseDto>>(Array.Empty<NotificationResponseDto>());

        public Task<bool> MarkReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> MarkAllReadAsync(string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
