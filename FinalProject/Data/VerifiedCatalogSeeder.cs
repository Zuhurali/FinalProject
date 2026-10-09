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

            var imageUrls = ImageUrls(vehicle);
            if (existing != null)
            {
                if (string.Equals(vehicle.Brand, "Ford", StringComparison.Ordinal)
                    && existing.IsNew != vehicle.IsNew)
                {
                    existing.IsNew = vehicle.IsNew;
                }

                var known = existing.Images
                    .Select(image => image.ImageUrl)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var order = existing.Images.Count == 0 ? 0 : existing.Images.Max(image => image.DisplayOrder) + 1;
                var hasMain = existing.Images.Any(image => image.IsMain);
                foreach (var imageUrl in imageUrls)
                {
                    if (!known.Add(imageUrl))
                    {
                        continue;
                    }

                    var isMain = !hasMain;
                    existing.Images.Add(new CarImage
                    {
                        ImageUrl = imageUrl,
                        IsMain = isMain,
                        DisplayOrder = order++
                    });
                    hasMain |= isMain;
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
                Images = imageUrls.Select((imageUrl, index) => new CarImage
                {
                    ImageUrl = imageUrl,
                    IsMain = index == 0,
                    DisplayOrder = index
                }).ToList()
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

    private static List<string> ImageUrls(VehicleSeed vehicle)
    {
        var urls = new List<string>();
        if (!string.IsNullOrWhiteSpace(vehicle.ImageUrl))
        {
            urls.Add(vehicle.ImageUrl);
        }

        if (vehicle.Gallery != null)
        {
            foreach (var imageUrl in vehicle.Gallery)
            {
                if (!string.IsNullOrWhiteSpace(imageUrl)
                    && !urls.Contains(imageUrl, StringComparer.OrdinalIgnoreCase))
                {
                    urls.Add(imageUrl);
                }
            }
        }

        return urls;
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
        string ImageUrl,
        string[]? Gallery = null);

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
        new("BMW", "i4 M50", "Electric", 2022, 49850m, 14760, "Electric", "Automatic", "Blue", "BMW i4 M50. 5 seats, all-wheel drive.", false, "/assets/images/catalog/bmw/i4-m50.jpg"),
        new("Nissan", "Altima", "Sedan", 2021, 18640m, 41280, "Petrol", "Automatic", "Beige", "Nissan Altima. 5 seats, front-wheel drive, 2.5L.", false, "/assets/images/catalog/nissan/altima.jpg"),
        new("Nissan", "Maxima", "Sedan", 2019, 22480m, 36750, "Petrol", "Automatic", "White", "Nissan Maxima. 5 seats, front-wheel drive, 3.5L V6.", false, "/assets/images/catalog/nissan/maxima.jpg"),
        new("Nissan", "Sentra", "Sedan", 2014, 10480m, 86420, "Petrol", "Automatic", "Silver", "Nissan Sentra. 5 seats, front-wheel drive, 1.8L.", false, "/assets/images/catalog/nissan/sentra.jpg"),
        new("Nissan", "Rogue", "SUV", 2024, 27840m, 12460, "Petrol", "Automatic", "White", "Nissan Rogue. 5 seats, all-wheel drive, 1.5L turbo.", false, "/assets/images/catalog/nissan/rogue.jpg"),
        new("Nissan", "Pathfinder", "SUV", 2023, 37920m, 16840, "Petrol", "Automatic", "Grey", "Nissan Pathfinder. 7 seats, four-wheel drive, 3.5L V6.", false, "/assets/images/catalog/nissan/pathfinder.jpg"),
        new("Nissan", "Murano", "SUV", 2019, 23680m, 41250, "Petrol", "Automatic", "Brown", "Nissan Murano. 5 seats, all-wheel drive, 3.5L V6.", false, "/assets/images/catalog/nissan/murano.jpg"),
        new("Nissan", "Armada", "SUV", 2023, 48750m, 18240, "Petrol", "Automatic", "Silver", "Nissan Armada. 8 seats, four-wheel drive, 5.6L V8.", false, "/assets/images/catalog/nissan/armada.jpg"),
        new("Nissan", "GT-R", "Sports Car", 2008, 58940m, 41280, "Petrol", "Automatic", "Black", "Nissan GT-R. 4 seats, all-wheel drive, 3.8L twin-turbo.", false, "/assets/images/catalog/nissan/gt-r.jpg"),
        new("Nissan", "Z", "Sports Car", 2023, 42860m, 6420, "Petrol", "Automatic", "Blue", "Nissan Z. 2 seats, rear-wheel drive, 3.0L twin-turbo.", false, "/assets/images/catalog/nissan/z.jpg"),
        new("Nissan", "Kicks", "SUV", 2024, 21870m, 11240, "Petrol", "Automatic", "Black", "Nissan Kicks. 5 seats, front-wheel drive, 1.6L.", false, "/assets/images/catalog/nissan/kicks.jpg"),
        new("Nissan", "Frontier", "SUV", 2022, 32470m, 28160, "Petrol", "Automatic", "Grey", "Nissan Frontier Pro-4X. 5 seats, four-wheel drive, 3.8L V6.", false, "/assets/images/catalog/nissan/frontier.jpg"),
        new("Ford", "Mustang GT", "Sports Car", 2018, 31480m, 32640, "Petrol", "Automatic", "Yellow", "Ford Mustang GT. 4 seats, rear-wheel drive, 5.0L V8.", false, "/assets/images/catalog/ford/mustang-gt.jpg"),
        new("Ford", "Mustang EcoBoost", "Coupe", 2024, 38960m, 6240, "Petrol", "Automatic", "White", "Ford Mustang EcoBoost. 4 seats, rear-wheel drive, 2.3L turbo.", true, "/assets/images/catalog/ford/mustang-ecoboost.jpg"),
        new("Ford", "Mustang Mach-E", "SUV", 2021, 29840m, 28460, "Electric", "Automatic", "Grey", "Ford Mustang Mach-E. 5 seats, all-wheel drive, dual motor.", true, "/assets/images/catalog/ford/mustang-mach-e.jpg"),
        new("Ford", "F-150", "SUV", 2021, 34280m, 41860, "Petrol", "Automatic", "Grey", "Ford F-150 SuperCrew. 5 seats, rear-wheel drive, 3.3L V6.", false, "/assets/images/catalog/ford/f-150.jpg"),
        new("Ford", "Ranger", "SUV", 2023, 39750m, 18640, "Diesel", "Automatic", "Orange", "Ford Ranger Wildtrak. 5 seats, four-wheel drive, 2.0L diesel.", true, "/assets/images/catalog/ford/ranger.jpg"),
        new("Ford", "Bronco", "SUV", 2023, 46890m, 12480, "Petrol", "Automatic", "Grey", "Ford Bronco. 5 seats, four-wheel drive, 2.3L turbo.", true, "/assets/images/catalog/ford/bronco.jpg"),
        new("Ford", "Explorer", "SUV", 2020, 28940m, 41280, "Petrol", "Automatic", "Grey", "Ford Explorer. 7 seats, all-wheel drive, 2.3L turbo.", false, "/assets/images/catalog/ford/explorer.jpg"),
        new("Ford", "Escape", "SUV", 2023, 26480m, 16820, "Petrol", "Automatic", "Grey", "Ford Escape. 5 seats, front-wheel drive, 1.5L turbo.", false, "/assets/images/catalog/ford/escape.jpg"),
        new("Ford", "Edge", "SUV", 2018, 18750m, 48260, "Petrol", "Automatic", "White", "Ford Edge ST-Line. 5 seats, all-wheel drive, 2.0L turbo.", false, "/assets/images/catalog/ford/edge.jpg"),
        new("Ford", "Focus", "Sedan", 2019, 14280m, 52640, "Petrol", "Automatic", "White", "Ford Focus. 5 seats, front-wheel drive, 1.5L turbo.", false, "/assets/images/catalog/ford/focus.jpg"),
        new("Ford", "Fusion", "Sedan", 2019, 16480m, 41200, "Petrol", "Automatic", "Silver", "Ford Fusion. 5 seats, front-wheel drive, 1.5L turbo.", false, "/assets/images/catalog/ford/fusion.jpg"),
        new("Mazda", "MX-5", "Convertible", 2019, 27850m, 18420, "Petrol", "Manual", "Grey", "Mazda MX-5 Miata. 2 seats, rear-wheel drive, 2.0L.", false, "/assets/images/catalog/mazda/mx-5-1.jpg", new[] { "/assets/images/catalog/mazda/mx-5-2.jpg", "/assets/images/catalog/mazda/mx-5-3.jpg", "/assets/images/catalog/mazda/mx-5-4.jpg" }),
        new("Mazda", "Mazda3", "Sedan", 2021, 21480m, 28640, "Petrol", "Automatic", "Red", "Mazda3 Sedan. 5 seats, front-wheel drive, 2.5L.", false, "/assets/images/catalog/mazda/mazda3-1.jpg", new[] { "/assets/images/catalog/mazda/mazda3-2.jpg", "/assets/images/catalog/mazda/mazda3-3.jpg", "/assets/images/catalog/mazda/mazda3-4.jpg" }),
        new("Mazda", "Mazda3 Hatchback", "Hatchback", 2020, 19860m, 32140, "Petrol", "Automatic", "White", "Mazda3 Hatchback. 5 seats, front-wheel drive, 2.0L Skyactiv-X.", false, "/assets/images/catalog/mazda/mazda3h-2.jpg", new[] { "/assets/images/catalog/mazda/mazda3h-1.jpg", "/assets/images/catalog/mazda/mazda3h-4.jpg", "/assets/images/catalog/mazda/mazda3h-3.jpg" }),
        new("Mazda", "Mazda6", "Sedan", 2018, 18420m, 46820, "Diesel", "Automatic", "Red", "Mazda6. 5 seats, front-wheel drive, 2.2L diesel.", false, "/assets/images/catalog/mazda/mazda6-1.jpg", new[] { "/assets/images/catalog/mazda/mazda6-2.jpg", "/assets/images/catalog/mazda/mazda6-3.jpg", "/assets/images/catalog/mazda/mazda6-4.jpg" }),
        new("Mazda", "CX-3", "SUV", 2018, 16280m, 54210, "Petrol", "Automatic", "Red", "Mazda CX-3. 5 seats, front-wheel drive, 2.0L.", false, "/assets/images/catalog/mazda/cx3-1.jpg", new[] { "/assets/images/catalog/mazda/cx3-2.jpg", "/assets/images/catalog/mazda/cx3-3.jpg", "/assets/images/catalog/mazda/cx3-4.jpg" }),
        new("Mazda", "CX-30", "SUV", 2021, 24860m, 29480, "Petrol", "Automatic", "Red", "Mazda CX-30. 5 seats, all-wheel drive, 2.5L.", false, "/assets/images/catalog/mazda/cx30-1.jpg", new[] { "/assets/images/catalog/mazda/cx30-2.jpg", "/assets/images/catalog/mazda/cx30-3.jpg", "/assets/images/catalog/mazda/cx30-4.jpg" }),
        new("Mazda", "CX-5", "SUV", 2022, 28940m, 22160, "Petrol", "Automatic", "Blue", "Mazda CX-5. 5 seats, all-wheel drive, 2.5L.", false, "/assets/images/catalog/mazda/cx5-1.jpg", new[] { "/assets/images/catalog/mazda/cx5-2.jpg", "/assets/images/catalog/mazda/cx5-3.jpg", "/assets/images/catalog/mazda/cx5-4.jpg" }),
        new("Mazda", "CX-50", "SUV", 2024, 36480m, 8420, "Hybrid", "Automatic", "White", "Mazda CX-50 Hybrid. 5 seats, all-wheel drive, 2.5L hybrid.", false, "/assets/images/catalog/mazda/cx50-1.jpg", new[] { "/assets/images/catalog/mazda/cx50-2.jpg", "/assets/images/catalog/mazda/cx50-3.jpg", "/assets/images/catalog/mazda/cx50-4.jpg" }),
        new("Mazda", "CX-60", "SUV", 2023, 42860m, 14620, "Hybrid", "Automatic", "White", "Mazda CX-60. 5 seats, all-wheel drive, 2.5L plug-in hybrid.", false, "/assets/images/catalog/mazda/cx60-1.jpg", new[] { "/assets/images/catalog/mazda/cx60-2.jpg", "/assets/images/catalog/mazda/cx60-3.jpg", "/assets/images/catalog/mazda/cx60-4.jpg" }),
        new("Mazda", "CX-90", "SUV", 2024, 48960m, 2140, "Hybrid", "Automatic", "Blue", "Mazda CX-90. 6 seats, all-wheel drive, 3.3L turbo hybrid.", true, "/assets/images/catalog/mazda/cx90-1.jpg", new[] { "/assets/images/catalog/mazda/cx90-2.jpg", "/assets/images/catalog/mazda/cx90-3.jpg", "/assets/images/catalog/mazda/cx90-4.jpg" }),
        new("Mazda", "RX-8", "Sports Car", 2008, 11480m, 68420, "Petrol", "Manual", "Silver", "Mazda RX-8. 4 seats, rear-wheel drive, 1.3L rotary.", false, "/assets/images/catalog/mazda/rx8-1.jpg", new[] { "/assets/images/catalog/mazda/rx8-2.jpg", "/assets/images/catalog/mazda/rx8-3.jpg", "/assets/images/catalog/mazda/rx8-4.jpg" }),
        new("Mazda", "RX-7", "Sports Car", 2002, 54800m, 41280, "Petrol", "Manual", "Silver", "Mazda RX-7. 2 seats, rear-wheel drive, 1.3L twin-rotor.", false, "/assets/images/catalog/mazda/rx7-1.jpg", new[] { "/assets/images/catalog/mazda/rx7-4.jpg", "/assets/images/catalog/mazda/rx7-2.jpg", "/assets/images/catalog/mazda/rx7-3.jpg" }),
        new("Toyota", "Sienna", "Minivans", 2022, 42860m, 24680, "Hybrid", "Automatic", "Black", "Toyota Sienna Limited. 7 seats, front-wheel drive, 2.5L hybrid.", false, "/assets/images/catalog/minivans/sienna.jpg"),
        new("Honda", "Odyssey", "Minivans", 2019, 28940m, 41280, "Petrol", "Automatic", "Black", "Honda Odyssey EX-L. 8 seats, front-wheel drive, 3.5L V6.", false, "/assets/images/catalog/minivans/odyssey.jpg"),
        new("Kia", "Carnival", "Minivans", 2023, 36480m, 16420, "Diesel", "Automatic", "White", "Kia Carnival SX. 8 seats, front-wheel drive, 2.2L diesel.", false, "/assets/images/catalog/minivans/carnival.jpg"),
        new("Chrysler", "Pacifica", "Minivans", 2022, 39240m, 8420, "Petrol", "Automatic", "Black", "Chrysler Pacifica. 7 seats, front-wheel drive, 3.6L V6.", true, "/assets/images/catalog/minivans/pacifica.jpg"),
        new("Chrysler", "Voyager", "Minivans", 2020, 24860m, 38640, "Petrol", "Automatic", "White", "Chrysler Voyager LX. 7 seats, front-wheel drive, 3.6L V6.", false, "/assets/images/catalog/minivans/voyager.jpg"),
        new("Volkswagen", "Multivan", "Minivans", 2022, 46890m, 12860, "Diesel", "Automatic", "Black", "Volkswagen Multivan Life. 7 seats, front-wheel drive, 2.0L diesel.", true, "/assets/images/catalog/minivans/multivan.jpg"),
        new("Volkswagen", "Caravelle", "Minivans", 2019, 34280m, 52480, "Petrol", "Automatic", "Black", "Volkswagen Caravelle. 8 seats, front-wheel drive, 2.0L TSI.", false, "/assets/images/catalog/minivans/caravelle.jpg"),
        new("Ford", "Tourneo Custom", "Minivans", 2023, 44280m, 4120, "Diesel", "Automatic", "Red", "Ford Tourneo Custom. 8 seats, front-wheel drive, 2.0L diesel.", true, "/assets/images/catalog/minivans/tourneo-custom.jpg"),
        new("Hyundai", "Staria", "Minivans", 2023, 39750m, 6240, "Diesel", "Automatic", "White", "Hyundai Staria Premium. 7 seats, front-wheel drive, 2.2L diesel.", true, "/assets/images/catalog/minivans/staria.jpg"),
        new("Mercedes-Benz", "V-Class", "Minivans", 2023, 62480m, 12840, "Diesel", "Automatic", "Grey", "Mercedes-Benz V-Class 300d. 7 seats, rear-wheel drive, 2.0L diesel.", false, "/assets/images/catalog/minivans/v-class.jpg")
    };
}
