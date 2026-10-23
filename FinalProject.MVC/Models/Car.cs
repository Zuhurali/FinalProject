namespace FinalProject.MVC.Models
{
    public class Car
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty; // Brand
        public string Model { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Mileage { get; set; }
        public string Fuel { get; set; } = string.Empty; // Petrol, Diesel, Electric, Hybrid
        public string Transmission { get; set; } = string.Empty; // Automatic, Manual
        public string ImagePath { get; set; } = string.Empty;
        public IReadOnlyList<string> GalleryImages { get; set; } = Array.Empty<string>();
        public bool IsNew { get; set; }
        public bool IsFeatured { get; set; }
        public string Badge { get; set; } = string.Empty; // New, Used (from IsNew)
        public int ImageCount { get; set; } = 1;
        public int Year { get; set; }
        public string Color { get; set; } = string.Empty;
        public int Seats { get; set; } = 5;
        public string Description { get; set; } = string.Empty;
        public bool InStock { get; set; } = true;
        public decimal MonthlyPayment { get; set; } // For financing
        public string DriveType { get; set; } = string.Empty;
        public string Engine { get; set; } = string.Empty;
        public bool DescriptionIsSpecSummary { get; set; }
        public string? OwnerId { get; set; }
        public string? OwnerDisplayName { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }
        public bool IsFavorite { get; set; }
    }

    public class CarFilterRequest
    {
        public string? Category { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
        public string? PriceRange { get; set; }
        public string? Fuel { get; set; }
        public string? Transmission { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int Page { get; set; } = 1;
    }

    public class CarFilterResponse
    {
        public List<Car> Cars { get; set; } = new List<Car>();
        public int TotalCars { get; set; }
        public string ResultsText { get; set; } = string.Empty;
    }
}
