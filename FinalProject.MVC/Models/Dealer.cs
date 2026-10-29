namespace FinalProject.MVC.Models
{
    public class Dealer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CardImage { get; set; } = string.Empty;
        public string DetailImage { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DescriptionExtra { get; set; } = string.Empty;
        /// <summary>Newline-separated company feature bullets shown on the dealer details page.</summary>
        public string FeatureList { get; set; } = string.Empty;
        public string CarType { get; set; } = string.Empty;
        public string Mileage { get; set; } = string.Empty;
        public string FuelType { get; set; } = string.Empty;
        public string Transmission { get; set; } = string.Empty;
        public string Person { get; set; } = string.Empty;
        public string DriveType { get; set; } = string.Empty;
        public string Engine { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string ContactRole { get; set; } = string.Empty;
        public string Whatsapp { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string SpecTransmission { get; set; } = string.Empty;
        public string ChargingTime { get; set; } = string.Empty;
        public string BatteryCapacity { get; set; } = string.Empty;
        public string FastCharge { get; set; } = string.Empty;
        public string BatteryWarranty { get; set; } = string.Empty;
        public string PowertrainWarranty { get; set; } = string.Empty;
        public string Displacement { get; set; } = string.Empty;
        public string Torque { get; set; } = string.Empty;
        public string Horsepower { get; set; } = string.Empty;
        public string Cylinders { get; set; } = string.Empty;
        public string EngineLayout { get; set; } = string.Empty;
        public string EngineConfig { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsPublished { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class DealerDetailsViewModel
    {
        public Dealer Dealer { get; set; } = new();
        public IReadOnlyList<Car> RelatedCars { get; set; } = Array.Empty<Car>();
    }
}
