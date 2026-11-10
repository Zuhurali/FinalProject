using FinalProject.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Data;

/// <summary>
/// API context. Migrations live in FinalProject/Data/Migrations and use __ApiMigrationsHistory.
/// Shop, sales, campaign, and Identity tables mapped here are excluded from these migrations.
/// </summary>
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

    public DbSet<ProductReviewRevision> ProductReviewRevisions => Set<ProductReviewRevision>();

    public DbSet<IdentityUserReference> IdentityUsers => Set<IdentityUserReference>();

    public DbSet<Favorite> Favorites => Set<Favorite>();

    public DbSet<ProductFavorite> ProductFavorites => Set<ProductFavorite>();

    public DbSet<CarView> CarViews => Set<CarView>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<ConversationHide> ConversationHides => Set<ConversationHide>();

    public DbSet<CarMessage> Messages => Set<CarMessage>();

    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    public DbSet<ShopProductReference> ShopProducts => Set<ShopProductReference>();

    public DbSet<ShowroomPurchaseRequestReference> ShowroomPurchaseRequests => Set<ShowroomPurchaseRequestReference>();

    public DbSet<InPersonSaleReference> InPersonSales => Set<InPersonSaleReference>();

    public DbSet<ProductCampaignReference> ProductCampaigns => Set<ProductCampaignReference>();

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
            entity.Property(category => category.IconPath).HasMaxLength(500).HasDefaultValue(string.Empty);
            entity.HasIndex(category => category.Name).IsUnique();
        });

        modelBuilder.Entity<Car>(entity =>
        {
            entity.Property(car => car.Model).IsRequired().HasMaxLength(100);
            entity.Property(car => car.FuelType).IsRequired().HasMaxLength(50);
            entity.Property(car => car.Transmission).IsRequired().HasMaxLength(50);
            entity.Property(car => car.Color).IsRequired().HasMaxLength(50);
            entity.Property(car => car.DriveType).HasMaxLength(20);
            entity.Property(car => car.Engine).HasMaxLength(80);
            entity.Property(car => car.Features).HasMaxLength(800);
            entity.Property(car => car.MileageUnit).HasMaxLength(8);
            entity.Property(car => car.Steering).HasMaxLength(8);
            entity.Property(car => car.Vin).HasMaxLength(17);
            entity.Property(car => car.AccidentHistory).HasMaxLength(40);
            entity.Property(car => car.RepaintedParts).HasMaxLength(400);
            entity.Property(car => car.ReplacedParts).HasMaxLength(400);
            entity.Property(car => car.CustomsStatus).HasMaxLength(40);
            entity.Property(car => car.MarketOrigin).HasMaxLength(80);
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
            // Historically non-nullable; keep DB required while CLR uses DateTime? from BaseEntity.
            entity.Property(rating => rating.UpdatedAt).IsRequired();
            entity.HasIndex(rating => new { rating.UserId, rating.ProductId })
                .IsUnique()
                .HasDatabaseName("UX_ProductRatings_UserId_ProductId");
            entity.HasIndex(rating => rating.ProductId)
                .HasDatabaseName("IX_ProductRatings_ProductId");
            // Presentation rows are not customer feedback and must never reach averages or counts.
            entity.HasQueryFilter(rating => !rating.IsDemo);
        });

        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.ToTable("ProductReviews", table => table.HasCheckConstraint(
                "CK_ProductReviews_ContentLength",
                "LEN([Content]) >= 1 AND LEN([Content]) <= 1000"));
            entity.Property(review => review.UserId).IsRequired().HasMaxLength(450);
            entity.Property(review => review.Content).IsRequired().HasMaxLength(1000);
            // Historically non-nullable; keep DB required while CLR uses DateTime? from BaseEntity.
            entity.Property(review => review.UpdatedAt).IsRequired();
            entity.HasIndex(review => review.ProductId)
                .HasDatabaseName("IX_ProductReviews_ProductId");
            entity.HasIndex(review => new { review.UserId, review.ProductId })
                .IsUnique()
                .HasDatabaseName("UX_ProductReviews_UserId_ProductId");
            entity.HasQueryFilter(review => !review.IsDemo);
        });

        modelBuilder.Entity<ProductReviewRevision>(entity =>
        {
            entity.ToTable("ProductReviewRevisions");
            entity.Property(revision => revision.PreviousContent).IsRequired().HasMaxLength(1000);
            entity.HasIndex(revision => revision.ReviewId);
            entity.HasOne<ProductReview>()
                .WithMany()
                .HasForeignKey(revision => revision.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);
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
            entity.Property(product => product.Id).ValueGeneratedOnAdd();
            entity.Property(product => product.Name).HasMaxLength(200).IsRequired();
            entity.Property(product => product.Price).HasColumnType("decimal(18,2)");
            entity.Property(product => product.Brand).HasMaxLength(100);
            entity.Property(product => product.PartNumber).HasMaxLength(80);
            entity.Property(product => product.Compatibility).HasMaxLength(300);
            entity.Property(product => product.Category).HasMaxLength(100).IsRequired();
            entity.Property(product => product.CarMake).HasMaxLength(100);
            entity.Property(product => product.CarModel).HasMaxLength(100);
            entity.Property(product => product.ImagePath).HasMaxLength(500).IsRequired();
            entity.Property(product => product.Description).HasMaxLength(2000);
            entity.Property(product => product.Color).HasMaxLength(50);
            entity.Property(product => product.Fuel).HasMaxLength(50);
            entity.Property(product => product.SearchKeywords).HasMaxLength(500);
        });

        modelBuilder.Entity<ShowroomPurchaseRequestReference>(entity =>
        {
            entity.ToTable("ShowroomPurchaseRequests", table => table.ExcludeFromMigrations());
            entity.HasKey(request => request.Id);
            entity.Property(request => request.CustomerName).HasMaxLength(80).IsRequired();
            entity.Property(request => request.Email).HasMaxLength(200).IsRequired();
            entity.Property(request => request.Phone).HasMaxLength(40).IsRequired();
            entity.Property(request => request.Note).HasMaxLength(500);
            entity.Property(request => request.Status).HasMaxLength(20).IsRequired();
            entity.Property(request => request.UserId).HasMaxLength(450);
            entity.Property(request => request.PreferredVisitDate).HasColumnType("date");
            entity.HasOne(request => request.Product)
                .WithMany()
                .HasForeignKey(request => request.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InPersonSaleReference>(entity =>
        {
            entity.ToTable("InPersonSales", table => table.ExcludeFromMigrations());
            entity.HasKey(sale => sale.Id);
            entity.Property(sale => sale.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(sale => sale.ListUnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(sale => sale.CampaignDiscountPercent).HasColumnType("decimal(5,2)");
            entity.Property(sale => sale.RecordedByUserId).HasMaxLength(450).IsRequired();
            entity.Property(sale => sale.Note).HasMaxLength(500);
            entity.Property(sale => sale.IdempotencyKey).HasMaxLength(64);
            entity.Property(sale => sale.VoidedByUserId).HasMaxLength(450);
            entity.HasIndex(sale => sale.IdempotencyKey)
                .IsUnique()
                .HasFilter("[IdempotencyKey] IS NOT NULL")
                .HasDatabaseName("UX_InPersonSales_IdempotencyKey");
            entity.HasOne(sale => sale.Product)
                .WithMany()
                .HasForeignKey(sale => sale.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductCampaignReference>(entity =>
        {
            entity.ToTable("ProductCampaigns", table => table.ExcludeFromMigrations());
            entity.HasKey(campaign => campaign.Id);
            entity.Property(campaign => campaign.Name).HasMaxLength(120);
            entity.Property(campaign => campaign.DiscountedPrice).HasColumnType("decimal(18,2)");
            entity.Property(campaign => campaign.DiscountPercent).HasColumnType("decimal(5,2)");
        });

        modelBuilder.Entity<CarView>(entity =>
        {
            entity.HasIndex(view => view.ViewedAt)
                .HasDatabaseName("IX_CarViews_ViewedAt");
            entity.HasIndex(view => new { view.CarId, view.ViewedAt })
                .HasDatabaseName("IX_CarViews_CarId_ViewedAt");
            entity.HasOne(view => view.Car)
                .WithMany()
                .HasForeignKey(view => view.CarId)
                .OnDelete(DeleteBehavior.Cascade);
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

        modelBuilder.Entity<ProductFavorite>(entity =>
        {
            entity.ToTable("ProductFavorites");
            entity.Property(favorite => favorite.UserId).IsRequired().HasMaxLength(450);
            entity.HasIndex(favorite => new { favorite.UserId, favorite.ProductId })
                .IsUnique()
                .HasDatabaseName("UX_ProductFavorites_UserId_ProductId");
            entity.HasIndex(favorite => favorite.ProductId);
            entity.HasOne(favorite => favorite.User)
                .WithMany()
                .HasForeignKey(favorite => favorite.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            // ShopProducts table is MVC-owned for schema ownership, but FK is enforced here for integrity.
            entity.HasOne(favorite => favorite.Product)
                .WithMany()
                .HasForeignKey(favorite => favorite.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.Property(conversation => conversation.BuyerId).IsRequired().HasMaxLength(450);
            entity.Property(conversation => conversation.SellerId).IsRequired().HasMaxLength(450);
            entity.Property(conversation => conversation.Subject).HasMaxLength(200);
            // Historically non-nullable; keep DB required while CLR uses DateTime? from BaseEntity.
            entity.Property(conversation => conversation.UpdatedAt).IsRequired();
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

        modelBuilder.Entity<ConversationHide>(entity =>
        {
            entity.Property(hide => hide.UserId).IsRequired().HasMaxLength(450);
            entity.HasIndex(hide => new { hide.ConversationId, hide.UserId })
                .IsUnique()
                .HasDatabaseName("UX_ConversationHides_ConversationId_UserId");
            entity.HasIndex(hide => hide.UserId)
                .HasDatabaseName("IX_ConversationHides_UserId");
            entity.HasOne(hide => hide.Conversation)
                .WithMany(conversation => conversation.Hides)
                .HasForeignKey(hide => hide.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(hide => hide.User)
                .WithMany()
                .HasForeignKey(hide => hide.UserId)
                .OnDelete(DeleteBehavior.Cascade);
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

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.Property(notification => notification.UserId).IsRequired().HasMaxLength(450);
            entity.Property(notification => notification.Type).IsRequired().HasMaxLength(40);
            entity.Property(notification => notification.Title).IsRequired().HasMaxLength(200);
            entity.Property(notification => notification.Body).IsRequired().HasMaxLength(1000);
            entity.Property(notification => notification.Url).IsRequired().HasMaxLength(500);
            entity.Property(notification => notification.DedupKey).HasMaxLength(200);
            entity.HasIndex(notification => new { notification.UserId, notification.IsRead })
                .HasDatabaseName("IX_UserNotifications_UserId_IsRead");
            entity.HasIndex(notification => notification.CreatedAt);
            entity.HasIndex(notification => new { notification.UserId, notification.DedupKey })
                .IsUnique()
                .HasFilter("[DedupKey] IS NOT NULL")
                .HasDatabaseName("UX_UserNotifications_UserId_DedupKey");
            entity.HasOne(notification => notification.User)
                .WithMany()
                .HasForeignKey(notification => notification.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        var seededAt = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Brand>().HasData(
            new Brand { Id = 1, Name = "BMW", CreatedAt = seededAt },
            new Brand { Id = 2, Name = "Mercedes-Benz", CreatedAt = seededAt },
            new Brand { Id = 3, Name = "Audi", CreatedAt = seededAt },
            new Brand { Id = 4, Name = "Honda", CreatedAt = seededAt },
            new Brand { Id = 5, Name = "Toyota", CreatedAt = seededAt });

        // Icon paths match the prior Home strip assignment (OrderBy Name → 01.svg…05.svg).
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Sedan", IconPath = "~/assets/images/category/04.svg", CreatedAt = seededAt },
            new Category { Id = 2, Name = "SUV", IconPath = "~/assets/images/category/05.svg", CreatedAt = seededAt },
            new Category { Id = 3, Name = "Coupe", IconPath = "~/assets/images/category/02.svg", CreatedAt = seededAt },
            new Category { Id = 4, Name = "Hatchback", IconPath = "~/assets/images/category/03.svg", CreatedAt = seededAt },
            new Category { Id = 5, Name = "Convertible", IconPath = "~/assets/images/category/01.svg", CreatedAt = seededAt });
    }
}
