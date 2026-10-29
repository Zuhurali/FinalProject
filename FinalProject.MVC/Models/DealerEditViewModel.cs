using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public sealed class DealerEditViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(500), Display(Name = "Card image path")]
    public string CardImage { get; set; } = string.Empty;

    [Required, MaxLength(500), Display(Name = "Detail image path")]
    public string DetailImage { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Summary { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Phone { get; set; } = string.Empty;

    [Required, MaxLength(200), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required, MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(2000), Display(Name = "Extra description")]
    public string DescriptionExtra { get; set; } = string.Empty;

    [Required, MaxLength(2000), Display(Name = "Feature list (one per line)")]
    public string FeatureList { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Car type")]
    public string CarType { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Mileage { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Fuel type")]
    public string FuelType { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Transmission { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Person { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Drive type")]
    public string DriveType { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Engine { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Color { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Contact name")]
    public string ContactName { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Contact role")]
    public string ContactRole { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Whatsapp { get; set; } = string.Empty;

    [Required, MaxLength(40), Display(Name = "Contact phone")]
    public string ContactPhone { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Spec transmission")]
    public string SpecTransmission { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Charging time")]
    public string ChargingTime { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Battery capacity")]
    public string BatteryCapacity { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Fast charge")]
    public string FastCharge { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Battery warranty")]
    public string BatteryWarranty { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Powertrain warranty")]
    public string PowertrainWarranty { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Displacement { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Torque { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Horsepower { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Cylinders { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Engine layout")]
    public string EngineLayout { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Engine config")]
    public string EngineConfig { get; set; } = string.Empty;

    [Display(Name = "Sort order")]
    [Range(0, 100000)]
    public int SortOrder { get; set; }

    [Display(Name = "Published")]
    public bool IsPublished { get; set; } = true;
}

public sealed class DealerImageUploads
{
    public IFormFile? CardImage { get; init; }
    public IFormFile? DetailImage { get; init; }
}
