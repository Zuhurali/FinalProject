using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models.Entities;

public class Brand : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Car> Cars { get; set; } = new List<Car>();
}
