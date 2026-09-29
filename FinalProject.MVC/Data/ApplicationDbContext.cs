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
        }
    }
}
