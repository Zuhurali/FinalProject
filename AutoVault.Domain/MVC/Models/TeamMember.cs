namespace FinalProject.MVC.Models
{
    public class TeamMember : BaseEntity
    {
        public TeamMember()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public string Name { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public string Biography { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsPublished { get; set; } = true;
    }
}
