using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models.Entities;

public class Brand
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public ICollection<Car> Cars { get; set; } = new List<Car>();
}
