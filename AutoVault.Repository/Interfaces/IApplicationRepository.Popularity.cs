namespace FinalProject.MVC.Repositories;

public partial interface IApplicationRepository
{
    Task<IReadOnlyList<ProductPopularityFeedbackRow>> GetProductPopularityFeedbackAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductPopularitySalesRow>> GetProductPopularitySalesAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductFavoriteCountRow>> GetProductFavoriteCountsAsync(
        CancellationToken cancellationToken = default);
    Task<int> DeleteDemoPopularitySalesAsync(string demoNote, string demoRecorderPrefix, CancellationToken cancellationToken = default);
}
