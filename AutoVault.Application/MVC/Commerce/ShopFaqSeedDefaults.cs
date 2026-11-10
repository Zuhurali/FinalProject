namespace FinalProject.MVC.Commerce;

public static class ShopFaqSeedDefaults
{
    public static IReadOnlyList<(string Question, string Answer)> Items { get; } =
    [
        (
            "Do you deliver parts or cars?",
            "No. AutoVault does not ship or offer courier delivery. You collect the car or part from the Baku store."),
        (
            "Can I pay on the website?",
            "No. There is no online checkout. You pay when you buy the item in person."),
        (
            "How do I know a part is available?",
            "Each product shows In Stock, Low Stock, or Out of Stock from the quantity stored for the store."),
        (
            "Do you reserve items online?",
            "No. You can send a purchase request from a product page, and our team will contact you to confirm availability. The item is not held, and the sale is completed at the store."),
        (
            "Where is the store?",
            "Visit us on Babek Prospekti 22–24 in Baku. Call before you travel if you want the team to confirm an item is still on the floor.")
    ];
}
