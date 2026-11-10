namespace FinalProject.MVC.Commerce;

/// <summary>
/// Fixed presentation sale counts. These are demonstration records, not verified in-store sales,
/// and are shown only to administrators.
/// </summary>
public static class PopularityDemoPlan
{
    public const int ReviewerCount = 10;
    public const string DemoSaleNote = "Demo in-store sale for the presentation. Not a verified sale.";

    public static string ReviewerId(int reviewerNumber) => $"demo-popularity-reviewer-{reviewerNumber:00}";

    public static int DemoSales(int productId)
    {
        return productId switch
        {
            1 => 8,
            2 => 16,
            3 => 9,
            4 => 11,
            5 => 6,
            6 => 12,
            7 => 20,
            8 => 4,
            9 => 10,
            10 => 13,
            11 => 15,
            12 => 7,
            13 => 6,
            14 => 18,
            15 => 8,
            16 => 48,
            17 => 9,
            18 => 7,
            19 => 14,
            20 => 11,
            21 => 1,
            _ => 5
        };
    }
}
