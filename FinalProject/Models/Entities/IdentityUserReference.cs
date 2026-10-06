namespace FinalProject.Models.Entities;

public class IdentityUserReference
{
    public string Id { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public string? Email { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}
