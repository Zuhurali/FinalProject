using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models.Entities;

public class Category
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Web-relative path to a category icon under wwwroot (e.g. ~/assets/images/category/01.svg).</summary>
    [MaxLength(500)]
    public string IconPath { get; set; } = string.Empty;

    public ICollection<Car> Cars { get; set; } = new List<Car>();
}
