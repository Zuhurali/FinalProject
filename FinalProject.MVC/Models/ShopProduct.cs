namespace FinalProject.MVC.Models
{
    public class ShopProduct
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Compatibility { get; set; } = string.Empty;
        public bool HasOffer { get; set; }
        public decimal SalePrice { get; set; }
        public int? DiscountPercent { get; set; }
        public DateTime? OfferEndsAt { get; set; }
        public string OfferEndLabel { get; set; } = string.Empty;
        public string OfferName { get; set; } = string.Empty;
        public decimal EffectivePrice => HasOffer ? SalePrice : Price;
        public string Category { get; set; } = string.Empty;
        public string CarMake { get; set; } = string.Empty;
        public string CarModel { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public double Rating { get; set; }
        public int RatingCount { get; set; }
        public int ReviewCount { get; set; }
        public int RealSalesCount { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public int DemoSalesCount { get; set; }
        public DateTime CreatedAt { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public double PopularityScore { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public int SalesCount => RealSalesCount + DemoSalesCount;
        public string Description { get; set; } = string.Empty;
        public bool InStock { get; set; } = true;
        public string Color { get; set; } = string.Empty;
        public string Fuel { get; set; } = string.Empty;
        public string SearchKeywords { get; set; } = string.Empty; // For search functionality
    }

    public class ShopFilterRequest
    {
        public string? CarMake { get; set; }
        public string? CarModel { get; set; }
        public string? PriceRange { get; set; }
        public string? SortBy { get; set; }
        public string? SearchText { get; set; }
        public string? Category { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public List<string>? SelectedMakes { get; set; }
        public List<string>? SelectedModels { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 6;
    }

    public class ShopFilterResponse
    {
        public List<ShopProduct> Products { get; set; } = new List<ShopProduct>();
        public int TotalProducts { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public bool HasMorePages { get; set; }
        public string ResultsText { get; set; } = string.Empty;
    }
}