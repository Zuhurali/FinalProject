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

    public DbSet<ProductRating> ProductRatings => Set<ProductRating>();

    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();

    public DbSet<IdentityUserReference> IdentityUsers => Set<IdentityUserReference>();

    public DbSet<Favorite> Favorites => Set<Favorite>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<CarMessage> Messages => Set<CarMessage>();

    public DbSet<ShopProductReference> ShopProducts => Set<ShopProductReference>();

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
            entity.Property(car => car.OwnerId).HasMaxLength(450);
            entity.Property(car => car.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Active");

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

            entity.HasOne(car => car.Owner)
                .WithMany()
                .HasForeignKey(car => car.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(car => car.OwnerId)
                .HasDatabaseName("IX_Cars_OwnerId");
        });

        modelBuilder.Entity<CarImage>(entity =>
        {
            entity.Property(image => image.ImageUrl).IsRequired().HasMaxLength(500);
        });

        modelBuilder.Entity<ProductRating>(entity =>
        {
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_ProductRatings_Rating",
                "[Rating] >= 1 AND [Rating] <= 5"));
            entity.Property(rating => rating.UserId).IsRequired().HasMaxLength(450);
            entity.Property(rating => rating.Rating).IsRequired();
            entity.HasIndex(rating => new { rating.UserId, rating.ProductId })
                .IsUnique()
                .HasDatabaseName("UX_ProductRatings_UserId_ProductId");
            entity.HasIndex(rating => rating.ProductId)
                .HasDatabaseName("IX_ProductRatings_ProductId");
        });

        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.ToTable("ProductReviews", table => table.HasCheckConstraint(
                "CK_ProductReviews_ContentLength",
                "LEN([Content]) >= 10 AND LEN([Content]) <= 1000"));
            entity.Property(review => review.UserId).IsRequired().HasMaxLength(450);
            entity.Property(review => review.Content).IsRequired().HasMaxLength(1000);
            entity.HasIndex(review => review.ProductId)
                .HasDatabaseName("IX_ProductReviews_ProductId");
            entity.HasIndex(review => new { review.UserId, review.ProductId })
                .IsUnique()
                .HasDatabaseName("UX_ProductReviews_UserId_ProductId");
        });

        modelBuilder.Entity<IdentityUserReference>(entity =>
        {
            entity.ToTable("AspNetUsers", table => table.ExcludeFromMigrations());
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasMaxLength(450);
            entity.Property(user => user.UserName).HasMaxLength(256);
            entity.Property(user => user.Email).HasMaxLength(256);
            entity.Property(user => user.FirstName).HasMaxLength(450);
            entity.Property(user => user.LastName).HasMaxLength(450);
        });

        modelBuilder.Entity<ShopProductReference>(entity =>
        {
            entity.ToTable("ShopProducts", table => table.ExcludeFromMigrations());
            entity.HasKey(product => product.Id);
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.Property(favorite => favorite.UserId).IsRequired().HasMaxLength(450);
            entity.HasIndex(favorite => new { favorite.UserId, favorite.CarId })
                .IsUnique()
                .HasDatabaseName("UX_Favorites_UserId_CarId");
            entity.HasOne(favorite => favorite.User)
                .WithMany()
                .HasForeignKey(favorite => favorite.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(favorite => favorite.Car)
                .WithMany(car => car.Favorites)
                .HasForeignKey(favorite => favorite.CarId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.Property(conversation => conversation.BuyerId).IsRequired().HasMaxLength(450);
            entity.Property(conversation => conversation.SellerId).IsRequired().HasMaxLength(450);
            entity.Property(conversation => conversation.Subject).HasMaxLength(200);
            entity.HasIndex(conversation => new
                {
                    conversation.CarId,
                    conversation.BuyerId,
                    conversation.SellerId
                })
                .IsUnique()
                .HasDatabaseName("UX_Conversations_CarId_BuyerId_SellerId");
            entity.HasIndex(conversation => conversation.UpdatedAt);
            entity.HasOne(conversation => conversation.Car)
                .WithMany(car => car.Conversations)
                .HasForeignKey(conversation => conversation.CarId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(conversation => conversation.Buyer)
                .WithMany()
                .HasForeignKey(conversation => conversation.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(conversation => conversation.Seller)
                .WithMany()
                .HasForeignKey(conversation => conversation.SellerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CarMessage>(entity =>
        {
            entity.Property(message => message.SenderId).IsRequired().HasMaxLength(450);
            entity.Property(message => message.ReceiverId).IsRequired().HasMaxLength(450);
            entity.Property(message => message.Content).IsRequired().HasMaxLength(4000);
            entity.HasIndex(message => new { message.ReceiverId, message.IsRead });
            entity.HasIndex(message => new { message.ConversationId, message.CreatedAt });
            entity.HasOne(message => message.Conversation)
                .WithMany(conversation => conversation.Messages)
                .HasForeignKey(message => message.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(message => message.Sender)
                .WithMany()
                .HasForeignKey(message => message.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(message => message.Receiver)
                .WithMany()
                .HasForeignKey(message => message.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
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
