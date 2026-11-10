using Microsoft.AspNetCore.Identity;

namespace FinalProject.MVC.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UTC time of the last successful password sign-in.</summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>UTC time of the last successful Change Password action.</summary>
        public DateTime? LastPasswordChangedAt { get; set; }

        /// <summary>UTC time of the last successful Forgot/Reset Password completion.</summary>
        public DateTime? LastPasswordResetAt { get; set; }

        /// <summary>
        /// Soft-delete flag. When true the account is deactivated: login is blocked and
        /// related historical records (cars, conversations, requests, etc.) are preserved.
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>UTC time when the Member soft-deleted their own account.</summary>
        public DateTime? DeletedAt { get; set; }

        public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
