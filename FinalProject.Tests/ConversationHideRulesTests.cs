using FinalProject.Commerce;
using Xunit;

namespace FinalProject.Tests;

public class ConversationHideRulesTests
{
    [Fact]
    public void IsHiddenForUser_is_true_only_for_matching_user()
    {
        var hides = new[]
        {
            (ConversationId: 7, UserId: "buyer-1"),
            (ConversationId: 8, UserId: "seller-1")
        };

        Assert.True(ConversationHideRules.IsHiddenForUser(hides, 7, "buyer-1"));
        Assert.False(ConversationHideRules.IsHiddenForUser(hides, 7, "seller-1"));
        Assert.False(ConversationHideRules.IsHiddenForUser(hides, 8, "buyer-1"));
    }

    [Fact]
    public void CanHideForUser_allows_buyer_and_seller()
    {
        Assert.True(ConversationHideRules.CanHideForUser(
            "buyer-1",
            "buyer-1",
            "seller-1",
            asStoreStaff: false,
            storeInboxUserId: null));
        Assert.True(ConversationHideRules.CanHideForUser(
            "seller-1",
            "buyer-1",
            "seller-1",
            asStoreStaff: false,
            storeInboxUserId: null));
    }

    [Fact]
    public void CanHideForUser_rejects_outsiders()
    {
        Assert.False(ConversationHideRules.CanHideForUser(
            "stranger",
            "buyer-1",
            "seller-1",
            asStoreStaff: false,
            storeInboxUserId: null));
    }

    [Fact]
    public void CanHideForUser_allows_store_staff_for_store_inbox_threads()
    {
        Assert.True(ConversationHideRules.CanHideForUser(
            "admin-1",
            "buyer-1",
            "store-inbox",
            asStoreStaff: true,
            storeInboxUserId: "store-inbox"));
        Assert.False(ConversationHideRules.CanHideForUser(
            "admin-1",
            "buyer-1",
            "private-seller",
            asStoreStaff: true,
            storeInboxUserId: "store-inbox"));
    }
}
