namespace FinalProject.MVC.Models;

public class DealerDetailsViewModel
{
    public Dealer Dealer { get; set; } = new();
    public IReadOnlyList<Car> RelatedCars { get; set; } = Array.Empty<Car>();
}
