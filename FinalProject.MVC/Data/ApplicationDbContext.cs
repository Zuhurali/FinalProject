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

        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<CouponUsage> CouponUsages { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<ShopCatalogProduct> ShopProducts { get; set; }
        public DbSet<ProductCampaign> ProductCampaigns { get; set; }
        public DbSet<InPersonSale> InPersonSales { get; set; }
        public DbSet<ProductInquiry> ProductInquiries { get; set; }
        public DbSet<ShowroomPurchaseRequest> ShowroomPurchaseRequests { get; set; }
        public DbSet<ContactRequest> ContactRequests { get; set; }

        public DbSet<SiteSettings> SiteSettings { get; set; }

        public DbSet<FaqItem> FaqItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<FaqItem>(entity =>
            {
                entity.Property(item => item.Question).HasMaxLength(300).IsRequired();
                entity.Property(item => item.Answer).HasMaxLength(4000).IsRequired();
                entity.HasIndex(item => item.SortOrder);
                entity.HasIndex(item => item.IsPublished);
                entity.ToTable("FaqItems", table =>
                {
                    table.HasCheckConstraint(
                        "CK_FaqItems_QuestionLength",
                        "LEN([Question]) >= 3 AND LEN([Question]) <= 300");
                    table.HasCheckConstraint(
                        "CK_FaqItems_AnswerLength",
                        "LEN([Answer]) >= 3 AND LEN([Answer]) <= 4000");
                });
            });

            builder.Entity<SiteSettings>(entity =>
            {
                entity.ToTable("SiteSettings");
                entity.HasKey(settings => settings.Id);
                entity.Property(settings => settings.Id).ValueGeneratedNever();
                entity.Property(settings => settings.SiteName).HasMaxLength(120).IsRequired();
                entity.Property(settings => settings.DisplayPhone).HasMaxLength(40).IsRequired();
                entity.Property(settings => settings.E164Phone).HasMaxLength(20).IsRequired();
                entity.Property(settings => settings.WhatsAppDigits).HasMaxLength(20).IsRequired();
                entity.Property(settings => settings.Email).HasMaxLength(200).IsRequired();
                entity.Property(settings => settings.AddressLine).HasMaxLength(300).IsRequired();
                entity.Property(settings => settings.MapsQuery).HasMaxLength(300).IsRequired();
                entity.Property(settings => settings.LogoPath).HasMaxLength(500);
                entity.Property(settings => settings.FooterLogoPath).HasMaxLength(500);
                entity.Property(settings => settings.FaviconPath).HasMaxLength(500);
                entity.Property(settings => settings.FooterText).HasMaxLength(500).IsRequired();
                entity.Property(settings => settings.CopyrightText).HasMaxLength(200).IsRequired();
                entity.Property(settings => settings.FacebookUrl).HasMaxLength(300);
                entity.Property(settings => settings.InstagramUrl).HasMaxLength(300);
                entity.Property(settings => settings.YoutubeUrl).HasMaxLength(300);
                entity.Property(settings => settings.TwitterUrl).HasMaxLength(300);
                entity.Property(settings => settings.DefaultSeoTitle).HasMaxLength(160).IsRequired();
                entity.Property(settings => settings.DefaultMetaDescription).HasMaxLength(320).IsRequired();
            });

            builder.Entity<Coupon>(entity =>
            {
                entity.Property(coupon => coupon.Code).HasMaxLength(50).IsRequired();
                entity.Property(coupon => coupon.Name).HasMaxLength(200).IsRequired();
                entity.Property(coupon => coupon.DiscountPercent).HasColumnType("decimal(5,2)");
                entity.Property(coupon => coupon.MinimumPurchaseAmount).HasColumnType("decimal(18,2)");
                entity.HasIndex(coupon => coupon.Code).IsUnique();
                entity.HasOne(coupon => coupon.Product)
                    .WithMany()
                    .HasForeignKey(coupon => coupon.ProductId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
                entity.ToTable("Coupons", table =>
                {
                    table.HasCheckConstraint(
                        "CK_Coupons_DiscountPercent",
                        "[DiscountPercent] > 0 AND [DiscountPercent] <= 100");
                    table.HasCheckConstraint(
                        "CK_Coupons_Scope",
                        "([AppliesToAllParts] = 1 AND [ProductId] IS NULL) OR ([AppliesToAllParts] = 0 AND [ProductId] IS NOT NULL)");
                    table.HasCheckConstraint(
                        "CK_Coupons_MaxUsesPerUser",
                        "[MaxUsesPerUser] >= 1");
                });
            });

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
                entity.Property(product => product.PartNumber).HasMaxLength(80);
                entity.Property(product => product.Compatibility).HasMaxLength(300);
                entity.HasIndex(product => product.PartNumber);
                entity.Property(product => product.IsActive).HasDefaultValue(true);
            });

            builder.Entity<ProductCampaign>(entity =>
            {
                entity.Property(campaign => campaign.Name).HasMaxLength(120).IsRequired();
                entity.Property(campaign => campaign.DiscountedPrice).HasColumnType("decimal(18,2)");
                entity.Property(campaign => campaign.DiscountPercent).HasColumnType("decimal(5,2)");
                entity.HasOne(campaign => campaign.Product)
                    .WithMany()
                    .HasForeignKey(campaign => campaign.ProductId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired(false);
                entity.HasIndex(campaign => campaign.ProductId);
                entity.HasIndex(campaign => campaign.AppliesToAllParts);
                entity.ToTable("ProductCampaigns", table => table.HasCheckConstraint(
                    "CK_ProductCampaigns_Scope",
                    "([AppliesToAllParts] = 1 AND [ProductId] IS NULL) OR ([AppliesToAllParts] = 0 AND [ProductId] IS NOT NULL)"));
            });

            builder.Entity<InPersonSale>(entity =>
            {
                entity.Property(sale => sale.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(sale => sale.ListUnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(sale => sale.CampaignDiscountPercent).HasColumnType("decimal(5,2)");
                entity.Property(sale => sale.RecordedByUserId).HasMaxLength(450).IsRequired();
                entity.Property(sale => sale.Note).HasMaxLength(500);
                entity.Property(sale => sale.VoidedByUserId).HasMaxLength(450);
                entity.HasOne(sale => sale.Product)
                    .WithMany()
                    .HasForeignKey(sale => sale.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(sale => sale.Campaign)
                    .WithMany()
                    .HasForeignKey(sale => sale.CampaignId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
                entity.HasIndex(sale => sale.VoidedAt);
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
                entity.Property(inquiry => inquiry.UserId).HasMaxLength(450);
                entity.HasIndex(inquiry => inquiry.ProductId);
                entity.HasIndex(inquiry => inquiry.UserId);
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
                entity.Property(request => request.UserId).HasMaxLength(450);
                entity.Property(request => request.PreferredVisitDate).HasColumnType("date");
                entity.HasOne(request => request.Product)
                    .WithMany()
                    .HasForeignKey(request => request.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(request => request.ProductId);
                entity.HasIndex(request => request.SubmissionKey).IsUnique();
                entity.HasIndex(request => request.UserId);
                entity.HasIndex(request => new { request.Email, request.ProductId, request.Status });
                entity.ToTable("ShowroomPurchaseRequests", table =>
                {
                    table.HasCheckConstraint("CK_ShowroomPurchaseRequests_Quantity", "[Quantity] >= 1 AND [Quantity] <= 10");
                    table.HasCheckConstraint("CK_ShowroomPurchaseRequests_Status", "[Status] IN (N'New', N'Contacted', N'Completed', N'Cancelled')");
                });
            });

            builder.Entity<ContactRequest>(entity =>
            {
                entity.Property(request => request.CustomerName).HasMaxLength(80).IsRequired();
                entity.Property(request => request.Email).HasMaxLength(200).IsRequired();
                entity.Property(request => request.Phone).HasMaxLength(40).IsRequired();
                entity.Property(request => request.Interest).HasMaxLength(120).IsRequired();
                entity.Property(request => request.Message).HasMaxLength(1000).IsRequired();
                entity.Property(request => request.Status).HasMaxLength(20).IsRequired();
                entity.Property(request => request.UserId).HasMaxLength(450);
                entity.HasIndex(request => request.CreatedAt);
                entity.HasIndex(request => request.Status);
                entity.HasIndex(request => request.UserId);
                entity.ToTable("ContactRequests", table =>
                {
                    table.HasCheckConstraint(
                        "CK_ContactRequests_MessageLength",
                        "LEN([Message]) >= 10 AND LEN([Message]) <= 1000");
                    table.HasCheckConstraint(
                        "CK_ContactRequests_Status",
                        "[Status] IN (N'New', N'In Progress', N'Resolved')");
                });
            });
        }
    }
}
