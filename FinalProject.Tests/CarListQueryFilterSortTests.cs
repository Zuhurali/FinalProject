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
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(7)]
    public async Task Seats_filter_returns_only_the_persisted_count(int seats)
    {
        await using var db = CreateDb();
        var match = await SeedAsync(db, "Match" + seats, seats: seats, description: "Listed as 9 seats in the description");
        await SeedAsync(db, "Other", seats: seats == 4 ? 2 : 4);
        await SeedAsync(db, "TextOnly", seats: null, description: $"{seats} seats, rear-wheel drive.");
        var service = new CarService(db, new SilentNotifications());

        var page = await service.QueryAsync(new CarListQueryDto
        {
            Seats = seats,
            Page = 1,
            PageSize = 50
        });

        Assert.Single(page.Items);
        Assert.Equal(match.Id, page.Items[0].Id);
        Assert.Equal(seats, page.Items[0].Seats);
    }

    [Fact]
    public async Task Seats_filter_matches_any_selected_persisted_count()
    {
        await using var db = CreateDb();
        var three = await SeedAsync(db, "Three", seats: 3, fuel: "Petrol");
        var four = await SeedAsync(db, "Four", seats: 4, fuel: "Diesel");
        await SeedAsync(db, "Five", seats: 5, fuel: "Petrol");
        await SeedAsync(db, "Eight", seats: 8);
        await SeedAsync(db, "Missing", seats: null, description: "A car with 3 seats written only in text");
        var service = new CarService(db, new SilentNotifications());

        var either = await service.QueryAsync(new CarListQueryDto
        {
            SeatCounts = [3, 4],
            Page = 1,
            PageSize = 50
        });
        Assert.Equal(new[] { three.Id, four.Id }.OrderBy(id => id), either.Items.Select(item => item.Id).OrderBy(id => id));

        var otherPair = await service.QueryAsync(new CarListQueryDto
        {
            SeatCounts = [5, 8],
            Page = 1,
            PageSize = 50
        });
        Assert.Equal(2, otherPair.TotalCount);
        Assert.All(otherPair.Items, item => Assert.Contains(item.Seats!.Value, new[] { 5, 8 }));

        var cleared = await service.QueryAsync(new CarListQueryDto { Page = 1, PageSize = 50 });
        Assert.Equal(5, cleared.TotalCount);

        var withFuel = await service.QueryAsync(new CarListQueryDto
        {
            SeatCounts = [3, 4],
            Fuel = "Petrol",
            Page = 1,
            PageSize = 50
        });
        Assert.Single(withFuel.Items);
        Assert.Equal(three.Id, withFuel.Items[0].Id);
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

    [Fact]
    public async Task Search_matches_make_model_and_description_keyword_separately()
    {
        await using var db = CreateDb();
        var toyota = await EnsureBrandAsync(db, "Toyota");
        var honda = await EnsureBrandAsync(db, "Honda");
        var byMake = await SeedAsync(db, "Camry", brandId: toyota.Id, description: "Daily sedan");
        var byModel = await SeedAsync(db, "Civic", brandId: honda.Id, description: "Commuter");
        var byKeyword = await SeedAsync(db, "Accord", brandId: honda.Id, description: "Sunroof and leather keyword");
        var service = new CarService(db, new SilentNotifications());

        var makePage = await service.QueryAsync(new CarListQueryDto { Search = "Toyota", Page = 1, PageSize = 50 });
        var modelPage = await service.QueryAsync(new CarListQueryDto { Search = "Civic", Page = 1, PageSize = 50 });
        var keywordPage = await service.QueryAsync(new CarListQueryDto { Search = "sunroof", Page = 1, PageSize = 50 });

        Assert.Equal(byMake.Id, Assert.Single(makePage.Items).Id);
        Assert.Equal(byModel.Id, Assert.Single(modelPage.Items).Id);
        Assert.Equal(byKeyword.Id, Assert.Single(keywordPage.Items).Id);
    }

    [Fact]
    public async Task Listing_text_search_matches_color_features_and_category_only_when_enabled()
    {
        await using var db = CreateDb();
        var coupe = await EnsureCategoryAsync(db, "Coupe");
        var sedan = await EnsureCategoryAsync(db, "Sedan");
        var byColor = await SeedAsync(db, "ColorCar", categoryId: sedan.Id, color: "Midnight Blue", description: "Plain");
        var byFeatures = await SeedAsync(db, "FeatureCar", categoryId: sedan.Id, features: "Heated seats and sunroof", description: "Plain");
        var byCategory = await SeedAsync(db, "CategoryCar", categoryId: coupe.Id, description: "Plain");
        var service = new CarService(db, new SilentNotifications());

        Assert.Empty((await service.QueryAsync(new CarListQueryDto { Search = "Midnight", Page = 1, PageSize = 20 })).Items);
        Assert.Empty((await service.QueryAsync(new CarListQueryDto { Search = "Heated", Page = 1, PageSize = 20 })).Items);
        Assert.Empty((await service.QueryAsync(new CarListQueryDto { Search = "Coupe", Page = 1, PageSize = 20 })).Items);

        Assert.Equal(byColor.Id, Assert.Single((await service.QueryAsync(new CarListQueryDto
        {
            Search = "midnight",
            IncludeListingText = true,
            Page = 1,
            PageSize = 20
        })).Items).Id);
        Assert.Equal(byFeatures.Id, Assert.Single((await service.QueryAsync(new CarListQueryDto
        {
            Search = "sunroof",
            IncludeListingText = true,
            Page = 1,
            PageSize = 20
        })).Items).Id);
        Assert.Equal(byCategory.Id, Assert.Single((await service.QueryAsync(new CarListQueryDto
        {
            Search = "coupe",
            IncludeListingText = true,
            Page = 1,
            PageSize = 20
        })).Items).Id);
    }

    [Fact]
    public async Task Listing_text_search_combines_with_filters_sort_and_pages()
    {
        await using var db = CreateDb();
        var matchLow = await SeedAsync(db, "One", color: "Pearl", fuel: "Petrol", price: 12000, description: "Pearl sedan");
        var matchHigh = await SeedAsync(db, "Two", color: "Pearl", fuel: "Petrol", price: 22000, description: "Pearl sedan");
        await SeedAsync(db, "Three", color: "Pearl", fuel: "Diesel", price: 15000, description: "Pearl diesel");
        await SeedAsync(db, "Four", color: "Black", fuel: "Petrol", price: 14000, description: "Other");
        var service = new CarService(db, new SilentNotifications());

        var query = new CarListQueryDto
        {
            Search = "Pearl",
            IncludeListingText = true,
            Fuel = "Petrol",
            MinPrice = 10000,
            MaxPrice = 25000,
            Status = CarPublicListing.ActiveStatus,
            Sort = "price-asc",
            PageSize = 1
        };

        var page1 = await service.QueryAsync(new CarListQueryDto
        {
            Search = query.Search,
            IncludeListingText = true,
            Fuel = query.Fuel,
            MinPrice = query.MinPrice,
            MaxPrice = query.MaxPrice,
            Status = query.Status,
            Sort = query.Sort,
            Page = 1,
            PageSize = 1
        });
        var page2 = await service.QueryAsync(new CarListQueryDto
        {
            Search = query.Search,
            IncludeListingText = true,
            Fuel = query.Fuel,
            MinPrice = query.MinPrice,
            MaxPrice = query.MaxPrice,
            Status = query.Status,
            Sort = query.Sort,
            Page = 2,
            PageSize = 1
        });

        Assert.Equal(2, page1.TotalCount);
        Assert.Equal(matchLow.Id, Assert.Single(page1.Items).Id);
        Assert.Equal(matchHigh.Id, Assert.Single(page2.Items).Id);
        Assert.NotEqual(page1.Items[0].Id, page2.Items[0].Id);
    }

    [Fact]
    public void Year_and_mileage_ranges_are_rejected_before_a_query()
    {
        Assert.Contains("minYear", new CarListQueryDto { MinYear = 2024, MaxYear = 2018 }.Validate()!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("minMileage", new CarListQueryDto { MinMileage = 90000, MaxMileage = 1000 }.Validate()!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("minPrice", new CarListQueryDto { MinPrice = 20, MaxPrice = 10 }.Validate()!, StringComparison.OrdinalIgnoreCase);
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
        string color = "Black",
        string? features = null,
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
            Color = color,
            Features = features,
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
