using FinalProject.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Repositories.Implementations;

public sealed partial class ApplicationRepository
{
    public HomePageContent? GetHomePageContentSingletonReadOnly() =>
        _db.HomePageContents.AsNoTracking().FirstOrDefault(c => c.Id == HomePageContent.SingletonId);

    public Task<bool> AnyHomePageContentSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.HomePageContents.AnyAsync(c => c.Id == HomePageContent.SingletonId, cancellationToken);

    public Task<HomePageContent?> FindTrackedHomePageContentSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.HomePageContents.FirstOrDefaultAsync(c => c.Id == HomePageContent.SingletonId, cancellationToken);

    public void AddHomePageContent(HomePageContent content) => _db.HomePageContents.Add(content);

    public AboutPageContent? GetAboutPageContentSingletonReadOnly() =>
        _db.AboutPageContents.AsNoTracking().FirstOrDefault(c => c.Id == AboutPageContent.SingletonId);

    public Task<bool> AnyAboutPageContentSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.AboutPageContents.AnyAsync(c => c.Id == AboutPageContent.SingletonId, cancellationToken);

    public Task<AboutPageContent?> FindTrackedAboutPageContentSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.AboutPageContents.FirstOrDefaultAsync(c => c.Id == AboutPageContent.SingletonId, cancellationToken);

    public void AddAboutPageContent(AboutPageContent content) => _db.AboutPageContents.Add(content);

    public ContactPageContent? GetContactPageContentSingletonReadOnly() =>
        _db.ContactPageContents.AsNoTracking().FirstOrDefault(c => c.Id == ContactPageContent.SingletonId);

    public Task<bool> AnyContactPageContentSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.ContactPageContents.AnyAsync(c => c.Id == ContactPageContent.SingletonId, cancellationToken);

    public Task<ContactPageContent?> FindTrackedContactPageContentSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.ContactPageContents.FirstOrDefaultAsync(c => c.Id == ContactPageContent.SingletonId, cancellationToken);

    public void AddContactPageContent(ContactPageContent content) => _db.ContactPageContents.Add(content);

    public LegalPageContent? GetLegalPageContentSingletonReadOnly() =>
        _db.LegalPageContents.AsNoTracking().FirstOrDefault(c => c.Id == LegalPageContent.SingletonId);

    public Task<bool> AnyLegalPageContentSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.LegalPageContents.AnyAsync(c => c.Id == LegalPageContent.SingletonId, cancellationToken);

    public Task<LegalPageContent?> FindTrackedLegalPageContentSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.LegalPageContents.FirstOrDefaultAsync(c => c.Id == LegalPageContent.SingletonId, cancellationToken);

    public void AddLegalPageContent(LegalPageContent content) => _db.LegalPageContents.Add(content);

    public ShopMarketingContent? GetShopMarketingContentSingletonReadOnly() =>
        _db.ShopMarketingContents.AsNoTracking().FirstOrDefault(c => c.Id == ShopMarketingContent.SingletonId);

    public Task<bool> AnyShopMarketingContentSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.ShopMarketingContents.AnyAsync(c => c.Id == ShopMarketingContent.SingletonId, cancellationToken);

    public Task<ShopMarketingContent?> FindTrackedShopMarketingContentSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.ShopMarketingContents.FirstOrDefaultAsync(c => c.Id == ShopMarketingContent.SingletonId, cancellationToken);

    public void AddShopMarketingContent(ShopMarketingContent content) => _db.ShopMarketingContents.Add(content);

    public FaqPageContent? GetFaqPageContentSingletonReadOnly() =>
        _db.FaqPageContents.AsNoTracking().FirstOrDefault(c => c.Id == FaqPageContent.SingletonId);

    public Task<bool> AnyFaqPageContentSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.FaqPageContents.AnyAsync(c => c.Id == FaqPageContent.SingletonId, cancellationToken);

    public Task<FaqPageContent?> FindTrackedFaqPageContentSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.FaqPageContents.FirstOrDefaultAsync(c => c.Id == FaqPageContent.SingletonId, cancellationToken);

    public void AddFaqPageContent(FaqPageContent content) => _db.FaqPageContents.Add(content);

    public TeamPageContent? GetTeamPageContentSingletonReadOnly() =>
        _db.TeamPageContents.AsNoTracking().FirstOrDefault(c => c.Id == TeamPageContent.SingletonId);

    public Task<bool> AnyTeamPageContentSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.TeamPageContents.AnyAsync(c => c.Id == TeamPageContent.SingletonId, cancellationToken);

    public Task<TeamPageContent?> FindTrackedTeamPageContentSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.TeamPageContents.FirstOrDefaultAsync(c => c.Id == TeamPageContent.SingletonId, cancellationToken);

    public void AddTeamPageContent(TeamPageContent content) => _db.TeamPageContents.Add(content);

    public DealerPageContent? GetDealerPageContentSingletonReadOnly() =>
        _db.DealerPageContents.AsNoTracking().FirstOrDefault(c => c.Id == DealerPageContent.SingletonId);

    public Task<bool> AnyDealerPageContentSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.DealerPageContents.AnyAsync(c => c.Id == DealerPageContent.SingletonId, cancellationToken);

    public Task<DealerPageContent?> FindTrackedDealerPageContentSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.DealerPageContents.FirstOrDefaultAsync(c => c.Id == DealerPageContent.SingletonId, cancellationToken);

    public void AddDealerPageContent(DealerPageContent content) => _db.DealerPageContents.Add(content);

    public CalculatorPageContent? GetCalculatorPageContentSingletonReadOnly() =>
        _db.CalculatorPageContents.AsNoTracking().FirstOrDefault(c => c.Id == CalculatorPageContent.SingletonId);

    public Task<bool> AnyCalculatorPageContentSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.CalculatorPageContents.AnyAsync(c => c.Id == CalculatorPageContent.SingletonId, cancellationToken);

    public Task<CalculatorPageContent?> FindTrackedCalculatorPageContentSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.CalculatorPageContents.FirstOrDefaultAsync(c => c.Id == CalculatorPageContent.SingletonId, cancellationToken);

    public void AddCalculatorPageContent(CalculatorPageContent content) => _db.CalculatorPageContents.Add(content);
}
