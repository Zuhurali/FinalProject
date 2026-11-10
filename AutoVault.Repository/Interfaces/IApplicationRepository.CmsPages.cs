using FinalProject.MVC.Models;

namespace FinalProject.MVC.Repositories;

public partial interface IApplicationRepository
{
    HomePageContent? GetHomePageContentSingletonReadOnly();
    Task<bool> AnyHomePageContentSingletonAsync(CancellationToken cancellationToken = default);
    Task<HomePageContent?> FindTrackedHomePageContentSingletonAsync(CancellationToken cancellationToken = default);
    void AddHomePageContent(HomePageContent content);

    AboutPageContent? GetAboutPageContentSingletonReadOnly();
    Task<bool> AnyAboutPageContentSingletonAsync(CancellationToken cancellationToken = default);
    Task<AboutPageContent?> FindTrackedAboutPageContentSingletonAsync(CancellationToken cancellationToken = default);
    void AddAboutPageContent(AboutPageContent content);

    ContactPageContent? GetContactPageContentSingletonReadOnly();
    Task<bool> AnyContactPageContentSingletonAsync(CancellationToken cancellationToken = default);
    Task<ContactPageContent?> FindTrackedContactPageContentSingletonAsync(CancellationToken cancellationToken = default);
    void AddContactPageContent(ContactPageContent content);

    LegalPageContent? GetLegalPageContentSingletonReadOnly();
    Task<bool> AnyLegalPageContentSingletonAsync(CancellationToken cancellationToken = default);
    Task<LegalPageContent?> FindTrackedLegalPageContentSingletonAsync(CancellationToken cancellationToken = default);
    void AddLegalPageContent(LegalPageContent content);

    ShopMarketingContent? GetShopMarketingContentSingletonReadOnly();
    Task<bool> AnyShopMarketingContentSingletonAsync(CancellationToken cancellationToken = default);
    Task<ShopMarketingContent?> FindTrackedShopMarketingContentSingletonAsync(CancellationToken cancellationToken = default);
    void AddShopMarketingContent(ShopMarketingContent content);

    FaqPageContent? GetFaqPageContentSingletonReadOnly();
    Task<bool> AnyFaqPageContentSingletonAsync(CancellationToken cancellationToken = default);
    Task<FaqPageContent?> FindTrackedFaqPageContentSingletonAsync(CancellationToken cancellationToken = default);
    void AddFaqPageContent(FaqPageContent content);

    TeamPageContent? GetTeamPageContentSingletonReadOnly();
    Task<bool> AnyTeamPageContentSingletonAsync(CancellationToken cancellationToken = default);
    Task<TeamPageContent?> FindTrackedTeamPageContentSingletonAsync(CancellationToken cancellationToken = default);
    void AddTeamPageContent(TeamPageContent content);

    DealerPageContent? GetDealerPageContentSingletonReadOnly();
    Task<bool> AnyDealerPageContentSingletonAsync(CancellationToken cancellationToken = default);
    Task<DealerPageContent?> FindTrackedDealerPageContentSingletonAsync(CancellationToken cancellationToken = default);
    void AddDealerPageContent(DealerPageContent content);

    CalculatorPageContent? GetCalculatorPageContentSingletonReadOnly();
    Task<bool> AnyCalculatorPageContentSingletonAsync(CancellationToken cancellationToken = default);
    Task<CalculatorPageContent?> FindTrackedCalculatorPageContentSingletonAsync(CancellationToken cancellationToken = default);
    void AddCalculatorPageContent(CalculatorPageContent content);
}
