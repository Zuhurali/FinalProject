using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models.Entities;

public class CarView
{
    public long Id { get; set; }

    public int CarId { get; set; }

    public DateTime ViewedAt { get; set; }

    public Car Car { get; set; } = null!;
}
