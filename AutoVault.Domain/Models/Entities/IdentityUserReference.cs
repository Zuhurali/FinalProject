namespace FinalProject.Models.Entities;

public class IdentityUserReference
{
    public string Id { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public string? Email { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    /// <summary>
    /// Mirrors AspNetUsers.IsDeleted (owned by MVC Identity). Soft-deleted authors
    /// stay in the database so review/rating history remains intact.
    /// </summary>
    public bool IsDeleted { get; set; }
}
