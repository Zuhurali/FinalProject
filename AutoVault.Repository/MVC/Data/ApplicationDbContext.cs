using FinalProject.MVC.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Data
{
    /// <summary>
    /// MVC and Identity context. Migrations live in FinalProject.MVC/Migrations and use __EFMigrationsHistory.
    /// This context owns users, coupons, orders, spare parts, campaigns, sales, questions, and CMS content.
    /// </summary>
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

        public DbSet<HomePageContent> HomePageContents { get; set; }

        public DbSet<FaqItem> FaqItems { get; set; }

        public DbSet<AboutPageContent> AboutPageContents { get; set; }

        public DbSet<ContactPageContent> ContactPageContents { get; set; }

        public DbSet<LegalPageContent> LegalPageContents { get; set; }

        public DbSet<ShopMarketingContent> ShopMarketingContents { get; set; }

        public DbSet<TeamMember> TeamMembers { get; set; }

        public DbSet<Dealer> Dealers { get; set; }

        public DbSet<PartnerStripSettings> PartnerStripSettings { get; set; }

        public DbSet<PartnerStripItem> PartnerStripItems { get; set; }

        public DbSet<FaqPageContent> FaqPageContents { get; set; }

        public DbSet<TeamPageContent> TeamPageContents { get; set; }

        public DbSet<DealerPageContent> DealerPageContents { get; set; }

        public DbSet<CalculatorPageContent> CalculatorPageContents { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Historically non-nullable UpdatedAt columns: keep DB required while CLR uses DateTime? from BaseEntity.
            foreach (var entityType in new[]
                     {
                         typeof(FaqItem),
                         typeof(TeamMember),
                         typeof(Dealer),
                         typeof(PartnerStripItem),
                         typeof(PartnerStripSettings),
                         typeof(SiteSettings),
                         typeof(HomePageContent),
                         typeof(AboutPageContent),
                         typeof(ContactPageContent),
                         typeof(LegalPageContent),
                         typeof(ShopMarketingContent),
                         typeof(FaqPageContent),
                         typeof(TeamPageContent),
                         typeof(DealerPageContent),
                         typeof(CalculatorPageContent)
                     })
            {
                builder.Entity(entityType).Property(nameof(BaseEntity.UpdatedAt)).IsRequired();
            }

            builder.Entity<FaqItem>(entity =>
            {
                entity.Property(item => item.Question).HasMaxLength(300).IsRequired();
                entity.Property(item => item.Answer).HasMaxLength(4000).IsRequired();
                entity.HasIndex(item => item.SortOrder);
                entity.HasIndex(item => item.IsPublished);
                entity.HasIndex(item => item.ShowOnShop);
                entity.Property(item => item.ShowOnFaqPage).HasDefaultValue(true);
                entity.Property(item => item.ShowOnShop).HasDefaultValue(false);
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
                entity.Property(settings => settings.CallCtaHeadline).HasMaxLength(200).IsRequired();
            });

            builder.Entity<HomePageContent>(entity =>
            {
                entity.ToTable("HomePageContents");
                entity.HasKey(content => content.Id);
                entity.Property(content => content.Id).ValueGeneratedNever();
                entity.Property(content => content.HeroTitle).HasMaxLength(200).IsRequired();
                entity.Property(content => content.HeroHighlight).HasMaxLength(80).IsRequired();
                entity.Property(content => content.HeroImagePath).HasMaxLength(500).IsRequired();
                entity.Property(content => content.HeroImageAlt).HasMaxLength(200).IsRequired();
                entity.Property(content => content.CategoriesEyebrow).HasMaxLength(120).IsRequired();
                entity.Property(content => content.CategoriesHeadingBefore).HasMaxLength(120).IsRequired();
                entity.Property(content => content.CategoriesHeadingHighlight).HasMaxLength(80).IsRequired();
                entity.Property(content => content.CategoriesHeadingAfter).HasMaxLength(80);
                entity.Property(content => content.PortfolioEyebrow).HasMaxLength(120).IsRequired();
                entity.Property(content => content.PortfolioHeadingBefore).HasMaxLength(120).IsRequired();
                entity.Property(content => content.PortfolioHeadingHighlight).HasMaxLength(80).IsRequired();
                entity.Property(content => content.PortfolioTabNewLabel).HasMaxLength(80).IsRequired();
                entity.Property(content => content.PortfolioTabUsedLabel).HasMaxLength(80).IsRequired();
                entity.Property(content => content.PortfolioTabTrendingLabel).HasMaxLength(80).IsRequired();
                entity.Property(content => content.PortfolioTabStocksLabel).HasMaxLength(80).IsRequired();
                entity.Property(content => content.PortfolioEmptyNew).HasMaxLength(200).IsRequired();
                entity.Property(content => content.PortfolioEmptyUsed).HasMaxLength(200).IsRequired();
                entity.Property(content => content.PortfolioEmptyTrending).HasMaxLength(200).IsRequired();
                entity.Property(content => content.PortfolioEmptyStocks).HasMaxLength(200).IsRequired();
                entity.Property(content => content.CampaignsEyebrow).HasMaxLength(120).IsRequired();
                entity.Property(content => content.CampaignsHeadingBefore).HasMaxLength(120).IsRequired();
                entity.Property(content => content.CampaignsHeadingHighlight).HasMaxLength(80).IsRequired();
                entity.Property(content => content.AboutEyebrow).HasMaxLength(120).IsRequired();
                entity.Property(content => content.AboutHeadingBefore).HasMaxLength(200).IsRequired();
                entity.Property(content => content.AboutHeadingHighlight).HasMaxLength(80).IsRequired();
                entity.Property(content => content.AboutBody).HasMaxLength(1000).IsRequired();
                entity.Property(content => content.AboutCtaLabel).HasMaxLength(80).IsRequired();
                entity.Property(content => content.AboutImageMainPath).HasMaxLength(500).IsRequired();
                entity.Property(content => content.AboutImageFloat1Path).HasMaxLength(500).IsRequired();
                entity.Property(content => content.AboutImageFloat2Path).HasMaxLength(500).IsRequired();
                entity.Property(content => content.BrandsEyebrow).HasMaxLength(120).IsRequired();
                entity.Property(content => content.BrandsHeadingBefore).HasMaxLength(120).IsRequired();
                entity.Property(content => content.BrandsHeadingHighlight).HasMaxLength(80).IsRequired();
                entity.Property(content => content.WhyChooseEyebrow).HasMaxLength(120).IsRequired();
                entity.Property(content => content.WhyChooseHeadingBefore).HasMaxLength(120).IsRequired();
                entity.Property(content => content.WhyChooseHeadingHighlight).HasMaxLength(80).IsRequired();
                entity.Property(content => content.WhyChooseBackgroundPath).HasMaxLength(500).IsRequired();
                entity.Property(content => content.Feature1Title).HasMaxLength(120).IsRequired();
                entity.Property(content => content.Feature1Description).HasMaxLength(400).IsRequired();
                entity.Property(content => content.Feature1IconPath).HasMaxLength(500).IsRequired();
                entity.Property(content => content.Feature2Title).HasMaxLength(120).IsRequired();
                entity.Property(content => content.Feature2Description).HasMaxLength(400).IsRequired();
                entity.Property(content => content.Feature2IconPath).HasMaxLength(500).IsRequired();
                entity.Property(content => content.Feature3Title).HasMaxLength(120).IsRequired();
                entity.Property(content => content.Feature3Description).HasMaxLength(400).IsRequired();
                entity.Property(content => content.Feature3IconPath).HasMaxLength(500).IsRequired();
                entity.Property(content => content.Feature4Title).HasMaxLength(120).IsRequired();
                entity.Property(content => content.Feature4Description).HasMaxLength(400).IsRequired();
                entity.Property(content => content.Feature4IconPath).HasMaxLength(500).IsRequired();
                entity.Property(content => content.FeaturedEyebrow).HasMaxLength(120).IsRequired();
                entity.Property(content => content.FeaturedHeadingBefore).HasMaxLength(120).IsRequired();
                entity.Property(content => content.FeaturedHeadingHighlight).HasMaxLength(80).IsRequired();
            });

            builder.Entity<AboutPageContent>(entity =>
            {
                entity.ToTable("AboutPageContents");
                entity.HasKey(content => content.Id);
                entity.Property(content => content.Id).ValueGeneratedNever();
            });

            builder.Entity<ContactPageContent>(entity =>
            {
                entity.ToTable("ContactPageContents");
                entity.HasKey(content => content.Id);
                entity.Property(content => content.Id).ValueGeneratedNever();
                entity.Property(content => content.MapEmbedUrl).HasMaxLength(1000).IsRequired();
            });

            builder.Entity<LegalPageContent>(entity =>
            {
                entity.ToTable("LegalPageContents");
                entity.HasKey(content => content.Id);
                entity.Property(content => content.Id).ValueGeneratedNever();
                entity.Property(content => content.PrivacyBody).HasMaxLength(8000).IsRequired();
                entity.Property(content => content.TermsBody).HasMaxLength(8000).IsRequired();
            });

            builder.Entity<ShopMarketingContent>(entity =>
            {
                entity.ToTable("ShopMarketingContents");
                entity.HasKey(content => content.Id);
                entity.Property(content => content.Id).ValueGeneratedNever();
            });

            builder.Entity<TeamMember>(entity =>
            {
                entity.ToTable("TeamMembers");
                entity.Property(member => member.Name).HasMaxLength(120).IsRequired();
                entity.Property(member => member.Position).HasMaxLength(120).IsRequired();
                entity.Property(member => member.ImagePath).HasMaxLength(500).IsRequired();
                entity.Property(member => member.Biography).HasMaxLength(2000).IsRequired();
                entity.Property(member => member.Experience).HasMaxLength(200).IsRequired();
                entity.Property(member => member.Department).HasMaxLength(120).IsRequired();
                entity.Property(member => member.Phone).HasMaxLength(40).IsRequired();
                entity.Property(member => member.Email).HasMaxLength(200).IsRequired();
                entity.HasIndex(member => member.SortOrder);
                entity.HasIndex(member => member.IsPublished);
                entity.Property(member => member.IsPublished).HasDefaultValue(true);
            });

            builder.Entity<Dealer>(entity =>
            {
                entity.ToTable("Dealers");
                entity.Property(dealer => dealer.Name).HasMaxLength(200).IsRequired();
                entity.Property(dealer => dealer.CardImage).HasMaxLength(500).IsRequired();
                entity.Property(dealer => dealer.DetailImage).HasMaxLength(500).IsRequired();
                entity.Property(dealer => dealer.Summary).HasMaxLength(500).IsRequired();
                entity.Property(dealer => dealer.Phone).HasMaxLength(40).IsRequired();
                entity.Property(dealer => dealer.Email).HasMaxLength(200).IsRequired();
                entity.Property(dealer => dealer.Address).HasMaxLength(300).IsRequired();
                entity.Property(dealer => dealer.Description).HasMaxLength(4000).IsRequired();
                entity.Property(dealer => dealer.DescriptionExtra).HasMaxLength(2000).IsRequired();
                entity.Property(dealer => dealer.FeatureList).HasMaxLength(2000).IsRequired();
                entity.HasIndex(dealer => dealer.SortOrder);
                entity.HasIndex(dealer => dealer.IsPublished);
                entity.Property(dealer => dealer.IsPublished).HasDefaultValue(true);
            });

            builder.Entity<PartnerStripSettings>(entity =>
            {
                entity.ToTable("PartnerStripSettings");
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Id).ValueGeneratedNever();
            });

            builder.Entity<PartnerStripItem>(entity =>
            {
                entity.ToTable("PartnerStripItems");
                entity.Property(i => i.Name).HasMaxLength(120).IsRequired();
                entity.Property(i => i.LogoPath).HasMaxLength(500).IsRequired();
                entity.Property(i => i.LinkUrl).HasMaxLength(500);
                entity.HasIndex(i => i.SortOrder);
                entity.HasIndex(i => i.IsPublished);
                entity.Property(i => i.IsPublished).HasDefaultValue(true);
            });

            builder.Entity<FaqPageContent>(entity =>
            {
                entity.ToTable("FaqPageContents");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Id).ValueGeneratedNever();
                entity.Property(c => c.BannerImagePath).HasMaxLength(500).IsRequired();
            });

            builder.Entity<TeamPageContent>(entity =>
            {
                entity.ToTable("TeamPageContents");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Id).ValueGeneratedNever();
            });

            builder.Entity<DealerPageContent>(entity =>
            {
                entity.ToTable("DealerPageContents");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Id).ValueGeneratedNever();
            });

            builder.Entity<CalculatorPageContent>(entity =>
            {
                entity.ToTable("CalculatorPageContents");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Id).ValueGeneratedNever();
                entity.Property(c => c.IntroText).HasMaxLength(1000).IsRequired();
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

            builder.Entity<CouponUsage>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.UserId, e.CouponCode })
                      .IsUnique()
                      .HasDatabaseName("IX_CouponUsage_UserId_CouponCode");

                entity.HasOne(e => e.User)
                      .WithMany(u => u.CouponUsages)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

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
                entity.Property(sale => sale.IdempotencyKey).HasMaxLength(64);
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
                entity.HasOne(sale => sale.PurchaseRequest)
                    .WithMany()
                    .HasForeignKey(sale => sale.PurchaseRequestId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
                entity.HasIndex(sale => sale.VoidedAt);
                entity.HasIndex(sale => sale.PurchaseRequestId);
                entity.HasIndex(sale => sale.IdempotencyKey)
                    .IsUnique()
                    .HasFilter("[IdempotencyKey] IS NOT NULL")
                    .HasDatabaseName("UX_InPersonSales_IdempotencyKey");
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
                // One open request per email+product (race-safe with app-level check).
                entity.HasIndex(request => new { request.Email, request.ProductId })
                    .IsUnique()
                    .HasFilter("[Status] IN (N'New', N'Contacted')")
                    .HasDatabaseName("UX_ShowroomPurchaseRequests_OpenEmailProduct");
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
