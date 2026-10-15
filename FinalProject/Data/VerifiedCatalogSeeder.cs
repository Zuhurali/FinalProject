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

            if (existing == null)
            {
                var sameModelYear = await db.Cars
                    .Include(car => car.Images)
                    .Where(car => car.BrandId == brand.Id
                        && car.Model == vehicle.Model
                        && car.Year == vehicle.Year)
                    .ToListAsync(cancellationToken);
                if (sameModelYear.Count == 1)
                {
                    existing = sameModelYear[0];
                    existing.Color = vehicle.Color;
                }
            }

            var imageUrls = ImageUrls(vehicle);
            if (existing != null)
            {
                if (existing.CategoryId != category.Id)
                {
                    existing.CategoryId = category.Id;
                }

                if (string.Equals(vehicle.Brand, "Ford", StringComparison.Ordinal)
                    && existing.IsNew != vehicle.IsNew)
                {
                    existing.IsNew = vehicle.IsNew;
                }

                SyncImageSet(db, existing, imageUrls);
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

    private static void SyncImageSet(FinalProjectDbContext db, Car existing, IReadOnlyList<string> imageUrls)
    {
        var desired = imageUrls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var extras = existing.Images
            .Where(image => !desired.Contains(image.ImageUrl, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (extras.Count > 0)
        {
            db.RemoveRange(extras);
            foreach (var extra in extras)
            {
                existing.Images.Remove(extra);
            }
        }

        for (var index = 0; index < desired.Count; index++)
        {
            var url = desired[index];
            var image = existing.Images.FirstOrDefault(item =>
                string.Equals(item.ImageUrl, url, StringComparison.OrdinalIgnoreCase));
            if (image == null)
            {
                image = new CarImage { ImageUrl = url };
                existing.Images.Add(image);
            }

            image.IsMain = index == 0;
            image.DisplayOrder = index;
        }
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
        new("Ferrari", "California", "Convertible", 2018, 189000m, 14500, "Petrol", "Automatic", "Red", "Ferrari California", true, "/assets/images/catalog/convertible/ferrari-california-t-front.jpg",
        [
            "/assets/images/catalog/convertible/ferrari-california-t-rear-quarter.jpg",
            "/assets/images/catalog/convertible/ferrari-california-t-rear.jpg",
            "/assets/images/catalog/convertible/ferrari-california-dashboard.jpg",
            "/assets/images/catalog/convertible/ferrari-california-cabin.jpg",
            "/assets/images/catalog/convertible/ferrari-california-t-side.jpg"
        ]),
        new("Mercedes-Benz", "SL-Class", "Convertible", 2019, 92000m, 18200, "Petrol", "Automatic", "Black", "Mercedes-Benz SL-Class", false, "/assets/images/catalog/convertible/mercedes-sl-class-black.jpg",
        [
            "/assets/images/catalog/convertible/mercedes-sl-r231-dashboard.jpg",
            "/assets/images/catalog/convertible/mercedes-sl-r231-cabin.jpg"
        ]),
        new("Mercedes-Benz", "SL-Class", "Convertible", 2016, 78000m, 27400, "Petrol", "Automatic", "White", "Mercedes-Benz SL-Class", false, "/assets/images/catalog/convertible/mercedes-sl-class-white.jpg",
        [
            "/assets/images/catalog/convertible/mercedes-sl-class-white-rear.jpg",
            "/assets/images/catalog/convertible/mercedes-sl-r231-wheel.jpg",
            "/assets/images/catalog/convertible/mercedes-sl-r231-console.jpg"
        ]),
        new("Nissan", "350Z", "Coupe", 2008, 18500m, 46200, "Petrol", "Manual", "Orange", "Nissan 350Z", false, "/assets/images/catalog/coupe/nissan-350z-z33-front.jpg",
        [
            "/assets/images/catalog/coupe/nissan-350z-z33-rear.jpg",
            "/assets/images/catalog/coupe/nissan-350z-z33-interior.jpg",
            "/assets/images/catalog/coupe/nissan-350z-z33-cabin.jpg"
        ]),
        new("Lamborghini", "Diablo", "Sports Car", 1998, 265000m, 16800, "Petrol", "Manual", "Red", "Lamborghini Diablo", false, "/assets/images/catalog/sports-car/lamborghini-diablo-red.jpg"),
        new("Porsche", "911 GT3 RS", "Sports Car", 2019, 219000m, 6400, "Petrol", "Automatic", "Custom wrap", "Porsche 911 GT3 RS", true, "/assets/images/catalog/sports-car/porsche-911-gt3-rs-991-2-front.jpg"),
        new("Porsche", "911", "Coupe", 2011, 128000m, 24100, "Petrol", "Manual", "White", "Porsche 911", false, "/assets/images/catalog/coupe/porsche-911-997-front.jpg",
        [
            "/assets/images/catalog/coupe/porsche-911-997-interior.jpg"
        ]),
        new("Mercedes-Benz", "AMG GT", "Coupe", 2018, 118000m, 15300, "Petrol", "Automatic", "Blue", "Mercedes-AMG GT", true, "/assets/images/catalog/coupe/mercedes-amg-gt-c190-front.jpg",
        [
            "/assets/images/catalog/coupe/mercedes-amg-gt-c190-rear.jpg",
            "/assets/images/catalog/coupe/mercedes-amg-gt-c190-interior.jpg"
        ]),
        new("Maserati", "GranTurismo", "Luxury Cars", 2017, 74000m, 22800, "Petrol", "Automatic", "Blue", "Maserati GranTurismo", false, "/assets/images/catalog/luxury/maserati-granturismo-m145-front.jpg"),
        new("Nissan", "Leaf", "Hatchback", 2021, 21900m, 18600, "Electric", "Automatic", "Red", "Nissan Leaf", false, "/assets/images/catalog/hatchback/nissan-leaf-ze1-red.jpg", new[] { "/assets/images/catalog/hatchback/nissan-leaf-ze1-red-rear.jpg" }),
        new("Rolls-Royce", "Phantom", "Luxury Cars", 2022, 455000m, 3200, "Petrol", "Automatic", "Black", "Rolls-Royce Phantom", true, "/assets/images/catalog/luxury/rolls-royce-phantom-viii-front.jpg",
        [
            "/assets/images/catalog/luxury/rolls-royce-phantom-viii-rear.jpg",
            "/assets/images/catalog/luxury/rolls-royce-phantom-viii-interior.jpg"
        ]),
        new("Rolls-Royce", "Wraith", "Luxury Cars", 2020, 318000m, 8100, "Petrol", "Automatic", "White", "Rolls-Royce Wraith", true, "/assets/images/catalog/luxury/rolls-royce-wraith-white.jpg"),
        new("Tesla", "Model 3", "Sedan", 2023, 42900m, 11200, "Electric", "Automatic", "White", "Tesla Model 3", true, "/assets/images/catalog/electric/tesla-model-3-highland-2023-front.jpg",
        [
            "/assets/images/catalog/electric/tesla-model-3-highland-2023-rear-quarter.jpg",
            "/assets/images/catalog/electric/tesla-model-3-highland-2023-interior.jpg"
        ]),
        new("Toyota", "Highlander", "SUV", 2024, 46500m, 4800, "Petrol", "Automatic", "Silver", "Toyota Highlander", true, "/assets/images/catalog/suv/toyota-highlander-xu70-front.jpg",
        [
            "/assets/images/catalog/suv/toyota-highlander-xu70-interior.jpg"
        ]),
        new("BMW", "M340i", "Sedan", 2022, 48750m, 18420, "Petrol", "Automatic", "Black", "BMW 3 Series M340i. 5 seats, xDrive all-wheel drive.", false, "/assets/images/catalog/bmw/m340i.jpg", new[] { "/assets/images/catalog/bmw/bmw-g20-m340i-rear.jpg", "/assets/images/catalog/bmw/bmw-g20-lci-interior.jpg" }),
        new("BMW", "M3 Competition", "Sedan", 2022, 72900m, 12480, "Petrol", "Automatic", "Blue", "BMW M3 Competition. 5 seats, rear-wheel drive.", false, "/assets/images/catalog/bmw/m3-competition.jpg", new[] { "/assets/images/catalog/bmw/bmw-m3-g80-blue-rear.jpg" }),
        new("BMW", "M440i", "Coupe", 2022, 46980m, 21360, "Petrol", "Automatic", "Grey", "BMW 4 Series M440i. 4 seats, xDrive all-wheel drive.", false, "/assets/images/catalog/bmw/bmw-m440i-g22-front.jpg", new[] { "/assets/images/catalog/bmw/bmw-m440i-g22-rear.jpg", "/assets/images/catalog/bmw/bmw-m440i-g22-showroom-front.jpg", "/assets/images/catalog/bmw/bmw-m440i-g22-showroom-rear.jpg" }),
        new("BMW", "M4 Competition", "Coupe", 2021, 74250m, 9860, "Petrol", "Automatic", "Yellow", "BMW M4 Competition. 4 seats, rear-wheel drive.", false, "/assets/images/catalog/bmw/m4-competition.jpg", new[] { "/assets/images/catalog/bmw/bmw-m4-g82-2021-rear.jpg", "/assets/images/catalog/bmw/bmw-m4-g82-interior.jpg" }),
        new("BMW", "530i", "Sedan", 2020, 32480m, 34720, "Petrol", "Automatic", "White", "BMW 5 Series 530i. 5 seats, rear-wheel drive.", false, "/assets/images/catalog/bmw/bmw-530i-g30-white-front.jpg", new[] { "/assets/images/catalog/bmw/bmw-530i-g30-2019-rear.jpg", "/assets/images/catalog/bmw/bmw-g30-lci-interior.jpg" }),
        new("BMW", "M5 (F90)", "Sedan", 2020, 79450m, 21860, "Petrol", "Automatic", "Grey", "BMW M5 (F90). 5 seats, xDrive all-wheel drive.", false, "/assets/images/catalog/bmw/bmw-m5-f90-front.jpg", new[] { "/assets/images/catalog/bmw/bmw-m5-f90-rear.jpg" }),
        new("BMW", "740i", "Sedan", 2023, 81250m, 8420, "Petrol", "Automatic", "White", "BMW 7 Series 740i. 5 seats, rear-wheel drive.", false, "/assets/images/catalog/bmw/bmw-g70-740i-white-front.jpg", new[] { "/assets/images/catalog/bmw/bmw-g70-740i-white-rear.jpg", "/assets/images/catalog/bmw/bmw-g70-interior.jpg" }),
        new("BMW", "M850i", "Convertible", 2019, 62750m, 24180, "Petrol", "Automatic", "White", "BMW 8 Series M850i. 4 seats, xDrive all-wheel drive.", false, "/assets/images/catalog/bmw/m850i.jpg", new[] { "/assets/images/catalog/bmw/bmw-m850i-g14-2019-rear.jpg", "/assets/images/catalog/bmw/bmw-m850i-2019-interior.jpg" }),
        new("BMW", "X3 M40i", "SUV", 2023, 49870m, 16240, "Petrol", "Automatic", "Grey", "BMW X3 M40i. 5 seats, xDrive all-wheel drive.", false, "/assets/images/catalog/bmw/bmw-x3-m40i-g01-lci-front.jpg", new[] { "/assets/images/catalog/bmw/bmw-x3-m40i-g01-lci-rear.jpg", "/assets/images/catalog/bmw/bmw-x3-g01-interior.jpg" }),
        new("BMW", "X5 xDrive40i", "SUV", 2021, 51940m, 29870, "Petrol", "Automatic", "Grey", "BMW X5 xDrive40i. 5 seats, xDrive all-wheel drive.", false, "/assets/images/catalog/bmw/x5-xdrive40i.jpg", new[] { "/assets/images/catalog/bmw/bmw-x5-g05-2020-interior.jpg" }),
        new("BMW", "X5 M Competition", "SUV", 2021, 89750m, 19640, "Petrol", "Automatic", "Black", "BMW X5 M Competition. 5 seats, xDrive all-wheel drive.", false, "/assets/images/catalog/bmw/bmw-x5-m-competition-g05-black-front.jpg", new[] { "/assets/images/catalog/bmw/bmw-x5-m-competition-g05-black-rear.jpg", "/assets/images/catalog/bmw/bmw-x5-g05-m-sport-interior.jpg" }),
        new("BMW", "X6 M Competition", "SUV", 2022, 96400m, 14280, "Petrol", "Automatic", "Grey", "BMW X6 M Competition. 5 seats, xDrive all-wheel drive.", false, "/assets/images/catalog/bmw/x6-m-competition.jpg"),
        new("BMW", "Z4 M40i", "Convertible", 2023, 54890m, 9840, "Petrol", "Automatic", "White", "BMW Z4 M40i. 2 seats, rear-wheel drive.", false, "/assets/images/catalog/bmw/z4-m40i.jpg", new[] { "/assets/images/catalog/bmw/bmw-z4-m40i-g29-interior.jpg" }),
        new("BMW", "i4 M50", "Electric", 2022, 49850m, 14760, "Electric", "Automatic", "Blue", "BMW i4 M50. 5 seats, all-wheel drive.", false, "/assets/images/catalog/bmw/i4-m50.jpg", new[] { "/assets/images/catalog/bmw/bmw-i4-g26-interior.jpg" }),
        new("Nissan", "Altima", "Sedan", 2021, 18640m, 41280, "Petrol", "Automatic", "Beige", "Nissan Altima. 5 seats, front-wheel drive, 2.5L.", false, "/assets/images/catalog/nissan/altima.jpg", new[] { "/assets/images/catalog/nissan/altima-l34-interior.jpg" }),
        new("Nissan", "Maxima", "Sedan", 2019, 22480m, 36750, "Petrol", "Automatic", "White", "Nissan Maxima. 5 seats, front-wheel drive, 3.5L V6.", false, "/assets/images/catalog/nissan/maxima.jpg", new[] { "/assets/images/catalog/nissan/maxima-a36-interior.jpg" }),
        new("Nissan", "Sentra", "Sedan", 2014, 10480m, 86420, "Petrol", "Automatic", "Silver", "Nissan Sentra. 5 seats, front-wheel drive, 1.8L.", false, "/assets/images/catalog/nissan/sentra.jpg"),
        new("Nissan", "Rogue", "SUV", 2024, 27840m, 12460, "Petrol", "Automatic", "White", "Nissan Rogue. 5 seats, all-wheel drive, 1.5L turbo.", false, "/assets/images/catalog/nissan/rogue.jpg", new[] { "/assets/images/catalog/nissan/rogue-t33-interior.jpg" }),
        new("Nissan", "Pathfinder", "SUV", 2023, 37920m, 16840, "Petrol", "Automatic", "Grey", "Nissan Pathfinder. 7 seats, four-wheel drive, 3.5L V6.", false, "/assets/images/catalog/nissan/pathfinder.jpg", new[] { "/assets/images/catalog/nissan/pathfinder-r53-rear.jpg", "/assets/images/catalog/nissan/pathfinder-r53-china-interior.jpg" }),
        new("Nissan", "Murano", "SUV", 2019, 23680m, 41250, "Petrol", "Automatic", "Brown", "Nissan Murano. 5 seats, all-wheel drive, 3.5L V6.", false, "/assets/images/catalog/nissan/murano.jpg", new[] { "/assets/images/catalog/nissan/murano-z52-interior.jpg" }),
        new("Nissan", "Armada", "SUV", 2023, 48750m, 18240, "Petrol", "Automatic", "Silver", "Nissan Armada. 8 seats, four-wheel drive, 5.6L V8.", false, "/assets/images/catalog/nissan/armada.jpg", new[] { "/assets/images/catalog/nissan/armada-y62-2021-rear.jpg", "/assets/images/catalog/nissan/armada-y62-2023-interior.jpg" }),
        new("Nissan", "GT-R", "Sports Car", 2008, 58940m, 41280, "Petrol", "Automatic", "Black", "Nissan GT-R. 4 seats, all-wheel drive, 3.8L twin-turbo.", false, "/assets/images/catalog/nissan/gt-r.jpg", new[] { "/assets/images/catalog/nissan/gt-r-r35-2009-rear.jpg", "/assets/images/catalog/nissan/gt-r-r35-2014-interior.jpg" }),
        new("Nissan", "Z", "Coupe", 2023, 42860m, 6420, "Petrol", "Automatic", "Blue", "Nissan Z. 2 seats, rear-wheel drive, 3.0L twin-turbo.", false, "/assets/images/catalog/nissan/z.jpg", new[] { "/assets/images/catalog/nissan/z-rz34-interior.jpg" }),
        new("Nissan", "Kicks", "SUV", 2024, 21870m, 11240, "Petrol", "Automatic", "Black", "Nissan Kicks. 5 seats, front-wheel drive, 1.6L.", false, "/assets/images/catalog/nissan/kicks.jpg"),
        new("Nissan", "Frontier", "SUV", 2022, 32470m, 28160, "Petrol", "Automatic", "Grey", "Nissan Frontier Pro-4X. 5 seats, four-wheel drive, 3.8L V6.", false, "/assets/images/catalog/nissan/frontier.jpg", new[] { "/assets/images/catalog/nissan/frontier-d41-2022-interior.jpg" }),
        new("Ford", "Mustang GT", "Coupe", 2018, 31480m, 32640, "Petrol", "Automatic", "Yellow", "Ford Mustang GT. 4 seats, rear-wheel drive, 5.0L V8.", false, "/assets/images/catalog/ford/mustang-gt.jpg", new[] { "/assets/images/catalog/ford/mustang-gt-s550-2018-rear.jpg", "/assets/images/catalog/ford/mustang-gt-s550-2018-interior.jpg" }),
        new("Ford", "Mustang EcoBoost", "Coupe", 2024, 38960m, 6240, "Petrol", "Automatic", "White", "Ford Mustang EcoBoost. 4 seats, rear-wheel drive, 2.3L turbo.", true, "/assets/images/catalog/ford/mustang-ecoboost.jpg", new[] { "/assets/images/catalog/convertible/ford-mustang-s650-interior.jpg" }),
        new("Ford", "Mustang Mach-E", "SUV", 2021, 29840m, 28460, "Electric", "Automatic", "Grey", "Ford Mustang Mach-E. 5 seats, all-wheel drive, dual motor.", true, "/assets/images/catalog/ford/mustang-mach-e.jpg", new[] { "/assets/images/catalog/ford/mustang-mach-e-2021-interior.jpg" }),
        new("Ford", "F-150", "SUV", 2021, 34280m, 41860, "Petrol", "Automatic", "Grey", "Ford F-150 SuperCrew. 5 seats, rear-wheel drive, 3.3L V6.", false, "/assets/images/catalog/ford/f-150.jpg", new[] { "/assets/images/catalog/ford/f-150-p702-2021-rear.jpg", "/assets/images/catalog/ford/f-150-p702-2022-interior.jpg" }),
        new("Ford", "Ranger", "SUV", 2023, 39750m, 18640, "Diesel", "Automatic", "Orange", "Ford Ranger Wildtrak. 5 seats, four-wheel drive, 2.0L diesel.", true, "/assets/images/catalog/ford/ranger.jpg", new[] { "/assets/images/catalog/ford/ranger-p703-interior.jpg" }),
        new("Ford", "Bronco", "SUV", 2023, 46890m, 12480, "Petrol", "Automatic", "Grey", "Ford Bronco. 5 seats, four-wheel drive, 2.3L turbo.", true, "/assets/images/catalog/ford/bronco.jpg", new[] { "/assets/images/catalog/ford/bronco-u725-interior.jpg" }),
        new("Ford", "Explorer", "SUV", 2020, 28940m, 41280, "Petrol", "Automatic", "Grey", "Ford Explorer. 7 seats, all-wheel drive, 2.3L turbo.", false, "/assets/images/catalog/ford/explorer.jpg", new[] { "/assets/images/catalog/ford/explorer-u625-2022-interior.jpg" }),
        new("Ford", "Escape", "SUV", 2023, 26480m, 16820, "Petrol", "Automatic", "Grey", "Ford Escape. 5 seats, front-wheel drive, 1.5L turbo.", false, "/assets/images/catalog/ford/escape-2023-facelift-front.jpg", new[] { "/assets/images/catalog/ford/escape-fourth-generation-interior.jpg" }),
        new("Ford", "Edge", "SUV", 2018, 18750m, 48260, "Petrol", "Automatic", "White", "Ford Edge ST-Line. 5 seats, all-wheel drive, 2.0L turbo.", false, "/assets/images/catalog/ford/edge.jpg", new[] { "/assets/images/catalog/ford/edge-second-generation-2018-rear.jpg" }),
        new("Ford", "Focus", "Sedan", 2019, 14280m, 52640, "Petrol", "Automatic", "White", "Ford Focus. 5 seats, front-wheel drive, 1.5L turbo.", false, "/assets/images/catalog/ford/focus.jpg"),
        new("Ford", "Fusion", "Sedan", 2019, 16480m, 41200, "Petrol", "Automatic", "Silver", "Ford Fusion. 5 seats, front-wheel drive, 1.5L turbo.", false, "/assets/images/catalog/ford/fusion.jpg"),
        new("Mazda", "MX-5", "Convertible", 2019, 27850m, 18420, "Petrol", "Manual", "Grey", "Mazda MX-5 Miata. 2 seats, rear-wheel drive, 2.0L.", false, "/assets/images/catalog/mazda/mx-5-1.jpg", new[] { "/assets/images/catalog/mazda/mazda-mx5-nd-interior.jpg" }),
        new("Mazda", "Mazda3", "Sedan", 2021, 21480m, 28640, "Petrol", "Automatic", "Red", "Mazda3 Sedan. 5 seats, front-wheel drive, 2.5L.", false, "/assets/images/catalog/mazda/mazda3-1.jpg"),
        new("Mazda", "Mazda3 Hatchback", "Hatchback", 2020, 19860m, 32140, "Petrol", "Automatic", "White", "Mazda3 Hatchback. 5 seats, front-wheel drive, 2.0L Skyactiv-X.", false, "/assets/images/catalog/mazda/mazda3h-2.jpg", new[] { "/assets/images/catalog/mazda/mazda3h-1.jpg", "/assets/images/catalog/mazda/mazda3-bp-hatch-rear.jpg" }),
        new("Mazda", "Mazda6", "Sedan", 2018, 18420m, 46820, "Diesel", "Automatic", "Red", "Mazda6. 5 seats, front-wheel drive, 2.2L diesel.", false, "/assets/images/catalog/mazda/mazda6-1.jpg", new[] { "/assets/images/catalog/mazda/mazda6-3.jpg", "/assets/images/catalog/mazda/mazda6-4.jpg" }),
        new("Mazda", "CX-3", "SUV", 2018, 16280m, 54210, "Petrol", "Automatic", "Red", "Mazda CX-3. 5 seats, front-wheel drive, 2.0L.", false, "/assets/images/catalog/mazda/cx3-1.jpg", new[] { "/assets/images/catalog/mazda/cx3-4.jpg" }),
        new("Mazda", "CX-30", "SUV", 2021, 24860m, 29480, "Petrol", "Automatic", "Red", "Mazda CX-30. 5 seats, all-wheel drive, 2.5L.", false, "/assets/images/catalog/mazda/cx30-1.jpg", new[] { "/assets/images/catalog/mazda/cx30-3.jpg", "/assets/images/catalog/mazda/cx30-4.jpg" }),
        new("Mazda", "CX-5", "SUV", 2022, 28940m, 22160, "Petrol", "Automatic", "Blue", "Mazda CX-5. 5 seats, all-wheel drive, 2.5L.", false, "/assets/images/catalog/mazda/cx5-1.jpg", new[] { "/assets/images/catalog/mazda/cx5-2.jpg" }),
        new("Mazda", "CX-50", "SUV", 2024, 36480m, 8420, "Hybrid", "Automatic", "White", "Mazda CX-50 Hybrid. 5 seats, all-wheel drive, 2.5L hybrid.", false, "/assets/images/catalog/mazda/cx50-1.jpg", new[] { "/assets/images/catalog/mazda/cx50-2.jpg" }),
        new("Mazda", "CX-60", "SUV", 2023, 42860m, 14620, "Hybrid", "Automatic", "White", "Mazda CX-60. 5 seats, all-wheel drive, 2.5L plug-in hybrid.", false, "/assets/images/catalog/mazda/cx60-1.jpg", new[] { "/assets/images/catalog/mazda/cx60-2.jpg", "/assets/images/catalog/mazda/cx60-3.jpg", "/assets/images/catalog/mazda/cx60-4.jpg" }),
        new("Mazda", "CX-90", "SUV", 2024, 48960m, 2140, "Hybrid", "Automatic", "Blue", "Mazda CX-90. 6 seats, all-wheel drive, 3.3L turbo hybrid.", true, "/assets/images/catalog/mazda/cx90-1.jpg", new[] { "/assets/images/catalog/mazda/cx90-2.jpg", "/assets/images/catalog/mazda/cx90-4.jpg" }),
        new("Mazda", "RX-8", "Coupe", 2008, 11480m, 68420, "Petrol", "Manual", "Silver", "Mazda RX-8. 4 seats, rear-wheel drive, 1.3L rotary.", false, "/assets/images/catalog/mazda/rx8-1.jpg", new[] { "/assets/images/catalog/mazda/mazda-rx8-interior.jpg" }),
        new("Mazda", "RX-7", "Sports Car", 2002, 54800m, 41280, "Petrol", "Manual", "Silver", "Mazda RX-7. 2 seats, rear-wheel drive, 1.3L twin-rotor.", false, "/assets/images/catalog/mazda/rx7-1.jpg", new[] { "/assets/images/catalog/mazda/mazda-rx7-fd-interior.jpg", "/assets/images/catalog/mazda/rx7-3.jpg" }),
        new("Toyota", "Sienna", "Minivans", 2022, 42860m, 24680, "Hybrid", "Automatic", "Black", "Toyota Sienna Limited. 7 seats, front-wheel drive, 2.5L hybrid.", false, "/assets/images/catalog/minivans/sienna.jpg"),
        new("Honda", "Odyssey", "Minivans", 2019, 28940m, 41280, "Petrol", "Automatic", "Black", "Honda Odyssey EX-L. 8 seats, front-wheel drive, 3.5L V6.", false, "/assets/images/catalog/minivans/odyssey.jpg"),
        new("Kia", "Carnival", "Minivans", 2023, 36480m, 16420, "Diesel", "Automatic", "White", "Kia Carnival SX. 8 seats, front-wheel drive, 2.2L diesel.", false, "/assets/images/catalog/minivans/carnival.jpg"),
        new("Chrysler", "Pacifica", "Minivans", 2022, 39240m, 8420, "Petrol", "Automatic", "Black", "Chrysler Pacifica. 7 seats, front-wheel drive, 3.6L V6.", true, "/assets/images/catalog/minivans/pacifica.jpg", new[] { "/assets/images/catalog/minivans/chrysler-pacifica-ru-interior.jpg" }),
        new("Chrysler", "Voyager", "Minivans", 2020, 24860m, 38640, "Petrol", "Automatic", "White", "Chrysler Voyager LX. 7 seats, front-wheel drive, 3.6L V6.", false, "/assets/images/catalog/minivans/voyager.jpg"),
        new("Volkswagen", "Multivan", "Minivans", 2022, 46890m, 12860, "Diesel", "Automatic", "Black", "Volkswagen Multivan Life. 7 seats, front-wheel drive, 2.0L diesel.", true, "/assets/images/catalog/minivans/multivan.jpg"),
        new("Volkswagen", "Caravelle", "Minivans", 2019, 34280m, 52480, "Petrol", "Automatic", "Black", "Volkswagen Caravelle. 8 seats, front-wheel drive, 2.0L TSI.", false, "/assets/images/catalog/minivans/caravelle.jpg"),
        new("Ford", "Tourneo Custom", "Minivans", 2023, 44280m, 4120, "Diesel", "Automatic", "Red", "Ford Tourneo Custom. 8 seats, front-wheel drive, 2.0L diesel.", true, "/assets/images/catalog/minivans/tourneo-custom.jpg"),
        new("Hyundai", "Staria", "Minivans", 2023, 39750m, 6240, "Diesel", "Automatic", "White", "Hyundai Staria Premium. 7 seats, front-wheel drive, 2.2L diesel.", true, "/assets/images/catalog/minivans/staria.jpg"),
        new("Mercedes-Benz", "V-Class", "Minivans", 2023, 62480m, 12840, "Diesel", "Automatic", "Grey", "Mercedes-Benz V-Class 300d. 7 seats, rear-wheel drive, 2.0L diesel.", false, "/assets/images/catalog/minivans/v-class.jpg",
        [
            "/assets/images/catalog/minivans/v-class-dashboard.jpg",
            "/assets/images/catalog/minivans/v-class-rear-cabin.jpg"
        ]),
        new("Volkswagen", "Golf GTI", "Hatchback", 2024, 34860m, 6240, "Petrol", "Automatic", "Blue", "Volkswagen Golf GTI. 5 seats, front-wheel drive, 2.0L turbo.", true, "/assets/images/catalog/hatchback/golf-gti-1.jpg", new[] { "/assets/images/catalog/hatchback/volkswagen-golf-gti-2023-interior.jpg" }),
        new("Volkswagen", "Golf R", "Hatchback", 2020, 32940m, 28640, "Petrol", "Automatic", "White", "Volkswagen Golf R. 5 seats, all-wheel drive, 2.0L turbo.", false, "/assets/images/catalog/hatchback/golf-r-1.jpg", new[] { "/assets/images/catalog/hatchback/volkswagen-golf-r-mk7-rear.jpg" }),
        new("Honda", "Civic Type R", "Hatchback", 2018, 36480m, 24180, "Petrol", "Manual", "Blue", "Honda Civic Type R. 5 seats, front-wheel drive, 2.0L turbo.", false, "/assets/images/catalog/hatchback/civic-1.jpg", new[] { "/assets/images/catalog/hatchback/civic-2.jpg", "/assets/images/catalog/hatchback/honda-civic-type-r-fk8-interior.jpg" }),
        new("Toyota", "Corolla Hatchback", "Hatchback", 2023, 24680m, 12840, "Hybrid", "Automatic", "Black", "Toyota Corolla Hatchback. 5 seats, front-wheel drive, 1.8L hybrid.", false, "/assets/images/catalog/hatchback/corolla-1.jpg", new[] { "/assets/images/catalog/hatchback/corolla-2.jpg", "/assets/images/catalog/hatchback/toyota-corolla-e210-interior.jpg" }),
        new("Hyundai", "i30", "Hatchback", 2024, 22860m, 4120, "Petrol", "Automatic", "Grey", "Hyundai i30. 5 seats, front-wheel drive, 1.5L turbo.", true, "/assets/images/catalog/hatchback/i30-1.jpg", new[] { "/assets/images/catalog/hatchback/i30-2.jpg", "/assets/images/catalog/hatchback/hyundai-i30-pd-interior.jpg" }),
        new("Hyundai", "Veloster", "Hatchback", 2012, 11280m, 68420, "Petrol", "Automatic", "Red", "Hyundai Veloster. 4 seats, front-wheel drive, 1.6L.", false, "/assets/images/catalog/hatchback/veloster-1.jpg", new[] { "/assets/images/catalog/hatchback/hyundai-veloster-2012-red-rear.jpg" }),
        new("Ford", "Focus ST", "Hatchback", 2014, 14860m, 72480, "Petrol", "Manual", "Yellow", "Ford Focus ST. 5 seats, front-wheel drive, 2.0L turbo.", false, "/assets/images/catalog/hatchback/focus-1.jpg", new[] { "/assets/images/catalog/hatchback/focus-2.jpg" }),
        new("Peugeot", "308", "Hatchback", 2022, 27480m, 18640, "Petrol", "Automatic", "Blue", "Peugeot 308. 5 seats, front-wheel drive, 1.2L turbo.", false, "/assets/images/catalog/hatchback/308-1.jpg", new[] { "/assets/images/catalog/hatchback/308-2.jpg" }),
        new("Renault", "Megane", "Hatchback", 2018, 16480m, 41280, "Petrol", "Automatic", "Blue", "Renault Megane GT. 5 seats, front-wheel drive, 1.6L turbo.", false, "/assets/images/catalog/hatchback/megane-1.jpg", new[] { "/assets/images/catalog/hatchback/megane-2.jpg" }),
        new("Audi", "A3 Sportback", "Hatchback", 2017, 19840m, 46820, "Petrol", "Automatic", "Red", "Audi A3 Sportback. 5 seats, front-wheel drive, 1.4L turbo.", false, "/assets/images/catalog/hatchback/a3-1.jpg", new[] { "/assets/images/catalog/hatchback/a3-2.jpg", "/assets/images/catalog/hatchback/audi-a3-sportback-8v-facelift-interior.jpg" }),
        new("Mercedes-Benz", "S-Class", "Luxury Cars", 2021, 86480m, 18420, "Petrol", "Automatic", "Black", "Mercedes-Benz S-Class. 5 seats, all-wheel drive, 3.0L turbo.", false, "/assets/images/catalog/luxury/sclass-1.jpg",
        [
            "/assets/images/catalog/luxury/sclass-2.jpg",
            "/assets/images/catalog/luxury/sclass-w223-cabin.jpg"
        ]),
        new("Mercedes-Benz", "Maybach S-Class", "Luxury Cars", 2023, 168400m, 6240, "Petrol", "Automatic", "Black", "Mercedes-Maybach S-Class. 4 seats, all-wheel drive, 4.0L V8.", true, "/assets/images/catalog/luxury/maybach-1.jpg"),
        new("BMW", "750Li", "Luxury Cars", 2019, 54860m, 31240, "Petrol", "Automatic", "Grey", "BMW 7 Series 750Li. 5 seats, all-wheel drive, 4.4L V8.", false, "/assets/images/catalog/luxury/750li-1.jpg",
        [
            "/assets/images/catalog/luxury/bmw-g11-750li-interior.jpg",
            "/assets/images/catalog/luxury/750li-2.jpg"
        ]),
        new("BMW", "840i", "Luxury Cars", 2021, 72480m, 16420, "Petrol", "Automatic", "White", "BMW 8 Series Gran Coupe 840i. 5 seats, all-wheel drive, 3.0L turbo.", false, "/assets/images/catalog/luxury/840i-1.jpg", new[] { "/assets/images/catalog/luxury/bmw-840i-g16-rear.jpg" }),
        new("Audi", "A8", "Luxury Cars", 2021, 68940m, 21480, "Petrol", "Automatic", "Silver", "Audi A8. 5 seats, all-wheel drive, 3.0L turbo.", false, "/assets/images/catalog/luxury/audi-a8-d5-front.jpg",
        [
            "/assets/images/catalog/luxury/audi-a8-d5-interior.jpg",
            "/assets/images/catalog/luxury/audi-a8-d5-rear-seats.jpg"
        ]),
        new("Bentley", "Continental GT", "Luxury Cars", 2020, 168750m, 12840, "Petrol", "Automatic", "Green", "Bentley Continental GT. 4 seats, all-wheel drive, 4.0L V8.", false, "/assets/images/catalog/luxury/continental-1.jpg", new[] { "/assets/images/catalog/luxury/continental-2.jpg" }),
        new("Bentley", "Flying Spur", "Luxury Cars", 2024, 198600m, 2140, "Petrol", "Automatic", "Grey", "Bentley Flying Spur. 5 seats, all-wheel drive, 4.0L V8.", true, "/assets/images/catalog/luxury/spur-1.jpg", new[] { "/assets/images/catalog/luxury/bentley-flying-spur-mulliner-gohm-side.jpg", "/assets/images/catalog/luxury/bentley-flying-spur-mulliner-gohm-rear.jpg", "/assets/images/catalog/luxury/bentley-flying-spur-iii-interior.jpg" }),
        new("Porsche", "Panamera", "Luxury Cars", 2019, 72480m, 28640, "Petrol", "Automatic", "Silver", "Porsche Panamera Turbo. 4 seats, all-wheel drive, 4.0L V8.", false, "/assets/images/catalog/luxury/panamera-1.jpg", new[] { "/assets/images/catalog/luxury/panamera-2.jpg" }),
        new("Lexus", "LS", "Luxury Cars", 2018, 42860m, 41280, "Petrol", "Automatic", "White", "Lexus LS 500 F Sport. 5 seats, rear-wheel drive, 3.5L V6.", false, "/assets/images/catalog/luxury/ls-1.jpg", new[] { "/assets/images/catalog/luxury/ls-2.jpg" }),
        new("Genesis", "G90", "Luxury Cars", 2024, 86480m, 3180, "Petrol", "Automatic", "White", "Genesis G90. 5 seats, all-wheel drive, 3.5L V6.", true, "/assets/images/catalog/luxury/g90-1.jpg"),
        new("Porsche", "911 Cabriolet", "Convertible", 2024, 128400m, 4120, "Petrol", "Automatic", "Grey", "Porsche 911 Cabriolet. 4 seats, rear-wheel drive, 3.0L turbo.", true, "/assets/images/catalog/convertible/911cab-1.jpg", new[] { "/assets/images/catalog/convertible/911cab-2.jpg", "/assets/images/catalog/convertible/porsche-911-992-cabriolet-interior.jpg" }),
        new("Audi", "A5 Cabriolet", "Convertible", 2021, 42860m, 24680, "Petrol", "Automatic", "White", "Audi A5 Cabriolet. 4 seats, front-wheel drive, 2.0L turbo.", false, "/assets/images/catalog/convertible/a5-1.jpg", new[] { "/assets/images/catalog/convertible/audi-a5-f5-cabriolet-rear.jpg", "/assets/images/catalog/convertible/audi-a5-f5-cabriolet-interior.jpg" }),
        new("Mercedes-Benz", "E-Class Cabriolet", "Convertible", 2019, 46840m, 32480, "Petrol", "Automatic", "Black", "Mercedes-Benz E-Class Cabriolet. 4 seats, all-wheel drive, 3.0L turbo.", false, "/assets/images/catalog/convertible/eclass-1.jpg", new[] { "/assets/images/catalog/convertible/mercedes-e-class-a238-rear.jpg", "/assets/images/catalog/convertible/mercedes-e-class-a238-interior.jpg", "/assets/images/catalog/convertible/eclass-2.jpg" }),
        new("Ford", "Mustang Convertible", "Convertible", 2024, 52480m, 2860, "Petrol", "Automatic", "Grey", "Ford Mustang Convertible. 4 seats, rear-wheel drive, 5.0L V8.", true, "/assets/images/catalog/convertible/mustang-cab-1.jpg", new[] { "/assets/images/catalog/convertible/mustang-cab-2.jpg", "/assets/images/catalog/convertible/ford-mustang-s650-interior.jpg" }),
        new("Toyota", "GR86", "Coupe", 2023, 32480m, 8420, "Petrol", "Manual", "Red", "Toyota GR86. 4 seats, rear-wheel drive, 2.4L.", true, "/assets/images/catalog/coupe/gr86-1.jpg", new[] { "/assets/images/catalog/coupe/toyota-gr86-rz-interior.jpg" }),
        new("Audi", "TT", "Coupe", 2017, 24860m, 41280, "Petrol", "Automatic", "Grey", "Audi TT. 4 seats, all-wheel drive, 2.0L turbo.", false, "/assets/images/catalog/coupe/tt-1.jpg", new[] { "/assets/images/catalog/coupe/audi-tt-8s-rear.jpg", "/assets/images/catalog/coupe/audi-tt-8s-interior.jpg" }),
        new("Porsche", "718 Cayman", "Coupe", 2019, 54860m, 22140, "Petrol", "Manual", "Grey", "Porsche 718 Cayman S. 2 seats, rear-wheel drive, 2.5L turbo.", false, "/assets/images/catalog/coupe/porsche-718-cayman-front.jpg", new[] { "/assets/images/catalog/coupe/porsche-718-cayman-interior.jpg" }),
        new("Chevrolet", "Camaro SS", "Coupe", 2018, 32480m, 28640, "Petrol", "Automatic", "Black", "Chevrolet Camaro SS. 4 seats, rear-wheel drive, 6.2L V8.", false, "/assets/images/catalog/coupe/chevrolet-camaro-ss-2018-front.jpg", new[] { "/assets/images/catalog/coupe/camaro-1.jpg", "/assets/images/catalog/coupe/chevrolet-camaro-ss-interior.jpg" }),
        new("Dodge", "Challenger", "Coupe", 2018, 42860m, 24180, "Petrol", "Automatic", "Black", "Dodge Challenger SRT Hellcat. 4 seats, rear-wheel drive, 6.2L supercharged V8.", false, "/assets/images/catalog/coupe/dodge-challenger-srt-hellcat-front.jpg", new[] { "/assets/images/catalog/coupe/dodge-challenger-srt-hellcat-rear.jpg", "/assets/images/catalog/coupe/dodge-challenger-srt-hellcat-interior.jpg" }),
        new("Nissan", "370Z", "Coupe", 2020, 27480m, 18640, "Petrol", "Manual", "Grey", "Nissan 370Z. 2 seats, rear-wheel drive, 3.7L V6.", false, "/assets/images/catalog/coupe/370z-1.jpg", new[] { "/assets/images/catalog/coupe/nissan-370z-rear.jpg", "/assets/images/catalog/coupe/nissan-370z-z34-manual-interior.jpg" }),
        new("Lexus", "RC 350", "Coupe", 2017, 29840m, 36420, "Petrol", "Automatic", "Silver", "Lexus RC 350. 4 seats, rear-wheel drive, 3.5L V6.", false, "/assets/images/catalog/coupe/rc-1.jpg", new[] { "/assets/images/catalog/coupe/rc-2.jpg", "/assets/images/catalog/coupe/lexus-rc350-2018-interior.jpg" }),
        new("Tesla", "Model S", "Electric", 2021, 62480m, 28460, "Electric", "Automatic", "Blue", "Tesla Model S Long Range. 5 seats, all-wheel drive, dual motor.", false, "/assets/images/catalog/electric/tesla-model-s-refresh-blue-front.jpg", new[] { "/assets/images/catalog/electric/tesla-model-s-refresh-blue-rear.jpg", "/assets/images/catalog/electric/tesla-model-s-interior.jpg" }),
        new("Porsche", "Taycan", "Electric", 2024, 98480m, 2140, "Electric", "Automatic", "White", "Porsche Taycan. 4 seats, all-wheel drive, dual motor.", true, "/assets/images/catalog/electric/taycan-1.jpg", new[] { "/assets/images/catalog/electric/porsche-taycan-gts-interior.jpg" }),
        new("Audi", "e-tron GT", "Electric", 2022, 78480m, 14620, "Electric", "Automatic", "Grey", "Audi e-tron GT. 4 seats, all-wheel drive, dual motor.", false, "/assets/images/catalog/electric/etron-1.jpg", new[] { "/assets/images/catalog/electric/audi-etron-gt-2023-interior.jpg" }),
        new("Hyundai", "Ioniq 5", "Electric", 2023, 42860m, 8420, "Electric", "Automatic", "White", "Hyundai Ioniq 5. 5 seats, all-wheel drive, dual motor.", true, "/assets/images/catalog/electric/ioniq-1.jpg", new[] { "/assets/images/catalog/electric/hyundai-ioniq-5-rear.jpg", "/assets/images/catalog/electric/hyundai-ioniq-5-ne-interior.jpg" }),
        new("Kia", "EV6", "Electric", 2023, 44680m, 11240, "Electric", "Automatic", "Grey", "Kia EV6. 5 seats, all-wheel drive, dual motor.", false, "/assets/images/catalog/electric/ev6-1.jpg", new[] { "/assets/images/catalog/electric/kia-ev6-gt-line-interior.jpg" }),
        new("Nissan", "Ariya", "Electric", 2023, 39840m, 6240, "Electric", "Automatic", "Silver", "Nissan Ariya. 5 seats, front-wheel drive, single motor.", true, "/assets/images/catalog/electric/nissan-ariya-2023-front.jpg", new[] { "/assets/images/catalog/electric/nissan-ariya-2022-interior.jpg" }),
        new("Volkswagen", "ID.4", "Electric", 2022, 36480m, 18640, "Electric", "Automatic", "Red", "Volkswagen ID.4. 5 seats, rear-wheel drive, single motor.", false, "/assets/images/catalog/electric/id4-1.jpg", new[] { "/assets/images/catalog/electric/volkswagen-id4-pro-interior.jpg" }),
        new("Mercedes-Benz", "EQS", "Electric", 2022, 86480m, 12480, "Electric", "Automatic", "Grey", "Mercedes-Benz EQS 450+. 5 seats, rear-wheel drive, single motor.", false, "/assets/images/catalog/electric/eqs-1.jpg", new[] { "/assets/images/catalog/electric/mercedes-eqs-v297-rear.jpg", "/assets/images/catalog/electric/mercedes-eqs-450plus-interior.jpg" }),
        new("BMW", "iX", "Electric", 2023, 72840m, 9840, "Electric", "Automatic", "Black", "BMW iX. 5 seats, all-wheel drive, dual motor.", false, "/assets/images/catalog/electric/ix-1.jpg", new[] { "/assets/images/catalog/electric/bmw-ix-xdrive40-interior.jpg" }),
        new("Lamborghini", "Huracán", "Sports Car", 2018, 248600m, 18420, "Petrol", "Automatic", "Black", "Lamborghini Huracán. 2 seats, all-wheel drive, 5.2L V10.", false, "/assets/images/catalog/sports-car/huracan-1.jpg", new[] { "/assets/images/catalog/sports-car/lamborghini-huracan-interior.jpg" }),
        new("Lamborghini", "Aventador", "Sports Car", 2015, 389400m, 12640, "Petrol", "Automatic", "Black", "Lamborghini Aventador LP700-4. 2 seats, all-wheel drive, 6.5L V12.", false, "/assets/images/catalog/sports-car/lamborghini-aventador-lp700-pirelli-front.jpg", new[] { "/assets/images/catalog/sports-car/aventador-3.jpg" }),
        new("Ferrari", "488 GTB", "Sports Car", 2017, 268900m, 14280, "Petrol", "Automatic", "Red", "Ferrari 488 GTB. 2 seats, rear-wheel drive, 3.9L twin-turbo V8.", false, "/assets/images/catalog/sports-car/488-1.jpg", new[] { "/assets/images/catalog/sports-car/ferrari-488-rear.jpg", "/assets/images/catalog/sports-car/488-3.jpg", "/assets/images/catalog/sports-car/488-4.jpg" }),
        new("Ferrari", "F8 Tributo", "Sports Car", 2020, 329500m, 8640, "Petrol", "Automatic", "Red", "Ferrari F8 Tributo. 2 seats, rear-wheel drive, 3.9L twin-turbo V8.", false, "/assets/images/catalog/sports-car/f8-1.jpg", new[] { "/assets/images/catalog/sports-car/ferrari-f8-rear.jpg" }),
        new("McLaren", "720S", "Sports Car", 2018, 248750m, 16850, "Petrol", "Automatic", "Silver", "McLaren 720S. 2 seats, rear-wheel drive, 4.0L twin-turbo V8.", false, "/assets/images/catalog/sports-car/720s-1.jpg", new[] { "/assets/images/catalog/sports-car/mclaren-720s-rear.jpg" }),
        new("Porsche", "911 Turbo S", "Coupe", 2021, 214500m, 11240, "Petrol", "Automatic", "White", "Porsche 911 Turbo S. 4 seats, all-wheel drive, 3.8L twin-turbo flat-six.", false, "/assets/images/catalog/sports-car/turbo-s-1.jpg", new[] { "/assets/images/catalog/sports-car/porsche-992-turbo-s-rear.jpg" }),
        new("Chevrolet", "Corvette C8", "Sports Car", 2021, 78900m, 18640, "Petrol", "Automatic", "Red", "Chevrolet Corvette C8. 2 seats, rear-wheel drive, 6.2L V8.", false, "/assets/images/catalog/sports-car/corvette-1.jpg", new[] { "/assets/images/catalog/sports-car/chevrolet-corvette-c8-rear.jpg", "/assets/images/catalog/sports-car/chevrolet-corvette-c8-interior.jpg" })
    };
}
