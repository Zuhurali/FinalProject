using FinalProject.MVC.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<CouponUsage> CouponUsages { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<ShopCatalogProduct> ShopProducts { get; set; }
        public DbSet<ProductCampaign> ProductCampaigns { get; set; }
        public DbSet<InPersonSale> InPersonSales { get; set; }
        public DbSet<ProductInquiry> ProductInquiries { get; set; }
        public DbSet<ShowroomPurchaseRequest> ShowroomPurchaseRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure CouponUsage entity
            builder.Entity<CouponUsage>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Create unique index to prevent duplicate coupon usage by the same user
                entity.HasIndex(e => new { e.UserId, e.CouponCode })
                      .IsUnique()
                      .HasDatabaseName("IX_CouponUsage_UserId_CouponCode");

                // Configure relationship
                entity.HasOne(e => e.User)
                      .WithMany(u => u.CouponUsages)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Order entity
            builder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.Orders)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(e => e.OrderItems)
                      .WithOne(oi => oi.Order)
                      .HasForeignKey(oi => oi.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure OrderItem entity
            builder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            builder.Entity<ShopCatalogProduct>(entity =>
            {
                entity.ToTable("ShopProducts", table => table.HasCheckConstraint(
                    "CK_ShopProducts_Quantity",
                    "[Quantity] >= 0"));
                entity.Property(product => product.Name).HasMaxLength(200).IsRequired();
                entity.Property(product => product.Price).HasColumnType("decimal(18,2)");
                entity.Property(product => product.Category).HasMaxLength(100).IsRequired();
                entity.Property(product => product.CarMake).HasMaxLength(100);
                entity.Property(product => product.CarModel).HasMaxLength(100);
                entity.Property(product => product.ImagePath).HasMaxLength(500).IsRequired();
                entity.Property(product => product.Description).HasMaxLength(2000);
                entity.Property(product => product.Color).HasMaxLength(50);
                entity.Property(product => product.Fuel).HasMaxLength(50);
                entity.Property(product => product.SearchKeywords).HasMaxLength(500);
                entity.Property(product => product.Brand).HasMaxLength(100);
                entity.Property(product => product.Compatibility).HasMaxLength(300);
            });

            builder.Entity<ProductCampaign>(entity =>
            {
                entity.Property(campaign => campaign.Name).HasMaxLength(120).IsRequired();
                entity.Property(campaign => campaign.DiscountedPrice).HasColumnType("decimal(18,2)");
                entity.Property(campaign => campaign.DiscountPercent).HasColumnType("decimal(5,2)");
                entity.HasOne(campaign => campaign.Product)
                    .WithMany()
                    .HasForeignKey(campaign => campaign.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(campaign => campaign.ProductId);
            });

            builder.Entity<InPersonSale>(entity =>
            {
                entity.Property(sale => sale.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(sale => sale.RecordedByUserId).HasMaxLength(450).IsRequired();
                entity.Property(sale => sale.Note).HasMaxLength(500);
                entity.HasOne(sale => sale.Product)
                    .WithMany()
                    .HasForeignKey(sale => sale.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.ToTable("InPersonSales", table => table.HasCheckConstraint(
                    "CK_InPersonSales_Quantity",
                    "[Quantity] >= 1"));
            });

            builder.Entity<ProductInquiry>(entity =>
            {
                entity.Property(inquiry => inquiry.CustomerName).HasMaxLength(80).IsRequired();
                entity.Property(inquiry => inquiry.Email).HasMaxLength(200).IsRequired();
                entity.Property(inquiry => inquiry.Phone).HasMaxLength(40);
                entity.Property(inquiry => inquiry.Message).HasMaxLength(1000).IsRequired();
                entity.HasOne(inquiry => inquiry.Product)
                    .WithMany()
                    .HasForeignKey(inquiry => inquiry.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(inquiry => inquiry.ProductId);
                entity.ToTable("ProductInquiries", table => table.HasCheckConstraint(
                    "CK_ProductInquiries_MessageLength",
                    "LEN([Message]) >= 10 AND LEN([Message]) <= 1000"));
            });

            builder.Entity<ShowroomPurchaseRequest>(entity =>
            {
                entity.Property(request => request.CustomerName).HasMaxLength(80).IsRequired();
                entity.Property(request => request.Email).HasMaxLength(200).IsRequired();
                entity.Property(request => request.Phone).HasMaxLength(40).IsRequired();
                entity.Property(request => request.Note).HasMaxLength(500);
                entity.Property(request => request.Status).HasMaxLength(20).IsRequired();
                entity.Property(request => request.PreferredVisitDate).HasColumnType("date");
                entity.HasOne(request => request.Product)
                    .WithMany()
                    .HasForeignKey(request => request.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(request => request.ProductId);
                entity.HasIndex(request => request.SubmissionKey).IsUnique();
                entity.HasIndex(request => new { request.Email, request.ProductId, request.Status });
                entity.ToTable("ShowroomPurchaseRequests", table =>
                {
                    table.HasCheckConstraint("CK_ShowroomPurchaseRequests_Quantity", "[Quantity] >= 1 AND [Quantity] <= 10");
                    table.HasCheckConstraint("CK_ShowroomPurchaseRequests_Status", "[Status] IN (N'New', N'Contacted', N'Completed', N'Cancelled')");
                });
            });
        }
    }
}
