using FinalProject.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Data;

public class FinalProjectDbContext : DbContext
{
    public const string MigrationsHistoryTable = "__ApiMigrationsHistory";

    public FinalProjectDbContext(DbContextOptions<FinalProjectDbContext> options)
        : base(options)
    {
    }

    public DbSet<Car> Cars => Set<Car>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<CarImage> CarImages => Set<CarImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.Property(brand => brand.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(brand => brand.Name).IsUnique();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(category => category.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(category => category.Name).IsUnique();
        });

        modelBuilder.Entity<Car>(entity =>
        {
            entity.Property(car => car.Model).IsRequired().HasMaxLength(100);
            entity.Property(car => car.FuelType).IsRequired().HasMaxLength(50);
            entity.Property(car => car.Transmission).IsRequired().HasMaxLength(50);
            entity.Property(car => car.Color).IsRequired().HasMaxLength(50);
            entity.Property(car => car.Description).HasMaxLength(4000);
            entity.Property(car => car.Price).HasColumnType("decimal(18,2)");

            entity.HasOne(car => car.Brand)
                .WithMany(brand => brand.Cars)
                .HasForeignKey(car => car.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(car => car.Category)
                .WithMany(category => category.Cars)
                .HasForeignKey(car => car.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(car => car.Images)
                .WithOne(image => image.Car)
                .HasForeignKey(image => image.CarId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CarImage>(entity =>
        {
            entity.Property(image => image.ImageUrl).IsRequired().HasMaxLength(500);
        });

        var seededAt = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Brand>().HasData(
            new Brand { Id = 1, Name = "BMW", CreatedAt = seededAt },
            new Brand { Id = 2, Name = "Mercedes-Benz", CreatedAt = seededAt },
            new Brand { Id = 3, Name = "Audi", CreatedAt = seededAt },
            new Brand { Id = 4, Name = "Honda", CreatedAt = seededAt },
            new Brand { Id = 5, Name = "Toyota", CreatedAt = seededAt });

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Sedan" },
            new Category { Id = 2, Name = "SUV" },
            new Category { Id = 3, Name = "Coupe" },
            new Category { Id = 4, Name = "Hatchback" },
            new Category { Id = 5, Name = "Convertible" });
    }
}
