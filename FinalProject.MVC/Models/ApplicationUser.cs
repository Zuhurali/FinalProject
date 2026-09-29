using Microsoft.AspNetCore.Identity;

namespace FinalProject.MVC.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Additional user properties can be added here if needed
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();
    }
}
