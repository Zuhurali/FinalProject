using FinalProject.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Data;

public static class VerifiedCatalogSeeder
{
    private static readonly DateTime SeededAt = new(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc);

    public static async Task SeedAsync(FinalProjectDbContext db, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var brands = await EnsureBrandsAsync(db, cancellationToken);
        var categories = await EnsureCategoriesAsync(db, cancellationToken);

        foreach (var vehicle in Vehicles)
        {
            var brand = brands[vehicle.Brand];
            var category = categories[vehicle.Category];

            var existing = await db.Cars
                .Include(car => car.Images)
                .FirstOrDefaultAsync(
                    car => car.BrandId == brand.Id
                        && car.Model == vehicle.Model
                        && car.Year == vehicle.Year
                        && car.Color == vehicle.Color,
                    cancellationToken);

            if (existing != null)
            {
                if (existing.Images.All(image => image.ImageUrl != vehicle.ImageUrl))
                {
                    existing.Images.Add(MainImage(vehicle.ImageUrl));
                }

                continue;
            }

            db.Cars.Add(new Car
            {
                Brand = brand,
                Category = category,
                Model = vehicle.Model,
                Year = vehicle.Year,
                Price = vehicle.Price,
                Mileage = vehicle.Mileage,
                FuelType = vehicle.FuelType,
                Transmission = vehicle.Transmission,
                Color = vehicle.Color,
                Description = vehicle.Description,
                IsNew = vehicle.IsNew,
                IsFeatured = false,
                CreatedAt = SeededAt,
                Images = new List<CarImage> { MainImage(vehicle.ImageUrl) }
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<Dictionary<string, Brand>> EnsureBrandsAsync(
        FinalProjectDbContext db,
        CancellationToken cancellationToken)
    {
        var names = Vehicles.Select(vehicle => vehicle.Brand).Distinct().ToList();
        var existing = await db.Brands
            .Where(brand => names.Contains(brand.Name))
            .ToListAsync(cancellationToken);

        foreach (var name in names.Except(existing.Select(brand => brand.Name)))
        {
            var brand = new Brand { Name = name, CreatedAt = SeededAt };
            db.Brands.Add(brand);
            existing.Add(brand);
        }

        await db.SaveChangesAsync(cancellationToken);
        return existing.ToDictionary(brand => brand.Name);
    }

    private static async Task<Dictionary<string, Category>> EnsureCategoriesAsync(
        FinalProjectDbContext db,
        CancellationToken cancellationToken)
    {
        var names = Vehicles.Select(vehicle => vehicle.Category).Distinct().ToList();
        var existing = await db.Categories
            .Where(category => names.Contains(category.Name))
            .ToListAsync(cancellationToken);

        foreach (var name in names.Except(existing.Select(category => category.Name)))
        {
            var category = new Category { Name = name };
            db.Categories.Add(category);
            existing.Add(category);
        }

        await db.SaveChangesAsync(cancellationToken);
        return existing.ToDictionary(category => category.Name);
    }

    private static CarImage MainImage(string imageUrl)
    {
        return new CarImage
        {
            ImageUrl = imageUrl,
            IsMain = true,
            DisplayOrder = 0
        };
    }

    private sealed record VehicleSeed(
        string Brand,
        string Model,
        string Category,
        int Year,
        decimal Price,
        int Mileage,
        string FuelType,
        string Transmission,
        string Color,
        string Description,
        bool IsNew,
        string ImageUrl);

    private static readonly VehicleSeed[] Vehicles =
    {
        new("Ferrari", "California", "Convertible", 2018, 189000m, 14500, "Petrol", "Automatic", "Red", "Ferrari California", true, "/assets/images/portfolio/02.webp"),
        new("Mercedes-Benz", "SL-Class", "Convertible", 2019, 92000m, 18200, "Petrol", "Automatic", "Black", "Mercedes-Benz SL-Class", false, "/assets/images/portfolio/05.webp"),
        new("Mercedes-Benz", "SL-Class", "Convertible", 2016, 78000m, 27400, "Petrol", "Automatic", "White", "Mercedes-Benz SL-Class", false, "/assets/images/about/07.webp"),
        new("Nissan", "350Z", "Sports Car", 2008, 18500m, 46200, "Petrol", "Manual", "Black", "Nissan 350Z", false, "/assets/images/portfolio/08.webp"),
        new("Lamborghini", "Diablo", "Sports Car", 1998, 265000m, 16800, "Petrol", "Manual", "Red", "Lamborghini Diablo", false, "/assets/images/portfolio/09.webp"),
        new("Porsche", "911 GT3 RS", "Sports Car", 2019, 219000m, 6400, "Petrol", "Automatic", "Red", "Porsche 911 GT3 RS", true, "/assets/images/portfolio/24.webp"),
        new("Porsche", "911", "Sports Car", 2011, 128000m, 24100, "Petrol", "Manual", "Gray", "Porsche 911", false, "/assets/images/banner/banner-slider-bg-09.webp"),
        new("Mercedes-Benz", "AMG GT", "Sports Car", 2018, 118000m, 15300, "Petrol", "Automatic", "Black", "Mercedes-AMG GT", true, "/assets/images/portfolio/34.webp"),
        new("Maserati", "GranTurismo", "Sports Car", 2017, 74000m, 22800, "Petrol", "Automatic", "White", "Maserati GranTurismo", false, "/assets/images/banner/banner-slider-bg-08.webp"),
        new("Nissan", "Leaf", "Hatchback", 2021, 21900m, 18600, "Electric", "Automatic", "Red", "Nissan Leaf", false, "/assets/images/about/02.webp"),
        new("Rolls-Royce", "Phantom", "Luxury Cars", 2022, 455000m, 3200, "Petrol", "Automatic", "Black", "Rolls-Royce Phantom", true, "/assets/images/portfolio/06.webp"),
        new("Rolls-Royce", "Wraith", "Luxury Cars", 2020, 318000m, 8100, "Petrol", "Automatic", "White", "Rolls-Royce Wraith", true, "/assets/images/about/06.webp"),
        new("Tesla", "Model 3", "Sedan", 2023, 42900m, 11200, "Electric", "Automatic", "Red", "Tesla Model 3", true, "/assets/images/portfolio/29.webp"),
        new("Toyota", "Highlander", "SUV", 2024, 46500m, 4800, "Petrol", "Automatic", "Red", "Toyota Highlander", true, "/assets/images/blog/11.webp"),
        new("BMW", "330i", "Sedan", 2021, 31850m, 28640, "Petrol", "Automatic", "White", "BMW 3 Series 330i. 5 seats, rear-wheel drive.", false, "/assets/images/catalog/bmw/330i.jpg"),
        new("BMW", "M340i", "Sedan", 2022, 48750m, 18420, "Petrol", "Automatic", "Blue", "BMW 3 Series M340i. 5 seats, xDrive all-wheel drive.", false, "/assets/images/catalog/bmw/m340i.jpg"),
        new("BMW", "M3 Competition", "Sports Car", 2022, 72900m, 12480, "Petrol", "Automatic", "Blue", "BMW M3 Competition. 5 seats, rear-wheel drive.", false, "/assets/images/catalog/bmw/m3-competition.jpg"),
        new("BMW", "M440i", "Coupe", 2022, 46980m, 21360, "Petrol", "Automatic", "Grey", "BMW 4 Series M440i. 4 seats, xDrive all-wheel drive.", false, "/assets/images/catalog/bmw/m440i.jpg"),
        new("BMW", "M4 Competition", "Coupe", 2021, 74250m, 9860, "Petrol", "Automatic", "Yellow", "BMW M4 Competition. 4 seats, rear-wheel drive.", false, "/assets/images/catalog/bmw/m4-competition.jpg"),
        new("BMW", "530i", "Sedan", 2020, 32480m, 34720, "Petrol", "Automatic", "Grey", "BMW 5 Series 530i. 5 seats, rear-wheel drive.", false, "/assets/images/catalog/bmw/530i.jpg"),
        new("BMW", "M5 (F90)", "Sports Car", 2020, 79450m, 21860, "Petrol", "Automatic", "Grey", "BMW M5 (F90). 5 seats, xDrive all-wheel drive.", false, "/assets/images/catalog/bmw/m5-f90.jpg"),
        new("BMW", "740i", "Sedan", 2023, 81250m, 8420, "Petrol", "Automatic", "Grey", "BMW 7 Series 740i. 5 seats, rear-wheel drive.", false, "/assets/images/catalog/bmw/740i.jpg"),
        new("BMW", "M850i", "Convertible", 2019, 62750m, 24180, "Petrol", "Automatic", "White", "BMW 8 Series M850i. 4 seats, xDrive all-wheel drive.", false, "/assets/images/catalog/bmw/m850i.jpg"),
        new("BMW", "X3 M40i", "SUV", 2023, 49870m, 16240, "Petrol", "Automatic", "Blue", "BMW X3 M40i. 5 seats, xDrive all-wheel drive.", false, "/assets/images/catalog/bmw/x3-m40i.jpg"),
        new("BMW", "X5 xDrive40i", "SUV", 2021, 51940m, 29870, "Petrol", "Automatic", "Grey", "BMW X5 xDrive40i. 5 seats, xDrive all-wheel drive.", false, "/assets/images/catalog/bmw/x5-xdrive40i.jpg"),
        new("BMW", "X5 M Competition", "SUV", 2021, 89750m, 19640, "Petrol", "Automatic", "Green", "BMW X5 M Competition. 5 seats, xDrive all-wheel drive.", false, "/assets/images/catalog/bmw/x5-m-competition.jpg"),
        new("BMW", "X6 M Competition", "SUV", 2022, 96400m, 14280, "Petrol", "Automatic", "Grey", "BMW X6 M Competition. 5 seats, xDrive all-wheel drive.", false, "/assets/images/catalog/bmw/x6-m-competition.jpg"),
        new("BMW", "Z4 M40i", "Convertible", 2023, 54890m, 9840, "Petrol", "Automatic", "White", "BMW Z4 M40i. 2 seats, rear-wheel drive.", false, "/assets/images/catalog/bmw/z4-m40i.jpg"),
        new("BMW", "i4 M50", "Electric", 2022, 49850m, 14760, "Electric", "Automatic", "Blue", "BMW i4 M50. 5 seats, all-wheel drive.", false, "/assets/images/catalog/bmw/i4-m50.jpg")
    };
}
