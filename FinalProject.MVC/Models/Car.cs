namespace FinalProject.MVC.Models
{
    public class Car
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty; // Brand
        public string Model { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Mileage { get; set; }
        public string Fuel { get; set; } = string.Empty; // Petrol, Diesel, Electric, Hybrid
        public string Transmission { get; set; } = string.Empty; // Automatic, Manual
        public string ImagePath { get; set; } = string.Empty;
        public string Badge { get; set; } = string.Empty; // New, Stocks, Trending, etc.
        public int ImageCount { get; set; } = 7; // Number of gallery images
        public int Year { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool InStock { get; set; } = true;
        public decimal MonthlyPayment { get; set; } // For financing
    }

    public class CarFilterRequest
    {
        public string? Make { get; set; }
        public string? Model { get; set; }
        public string? PriceRange { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }

    public class CarFilterResponse
    {
        public List<Car> Cars { get; set; } = new List<Car>();
        public int TotalCars { get; set; }
        public string ResultsText { get; set; } = string.Empty;
    }
}
