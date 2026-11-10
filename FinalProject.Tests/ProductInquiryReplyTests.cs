using FinalProject.MVC.Models;
using Xunit;

namespace FinalProject.Tests;

public class ProductInquiryReplyTests
{
    [Fact]
    public void Editing_from_the_beginning_middle_or_end_replaces_the_saved_reply()
    {
        var inquiry = Answered("The part is on the shelf today.");

        Assert.Null(inquiry.TrySaveReply("A new reply from the beginning.", "admin-1"));
        Assert.Equal("A new reply from the beginning.", inquiry.AdminReply);

        Assert.Null(inquiry.TrySaveReply("The part is in the stock room today.", "admin-1"));
        Assert.Equal("The part is in the stock room today.", inquiry.AdminReply);
        Assert.DoesNotContain("shelf", inquiry.AdminReply, StringComparison.Ordinal);

        Assert.Null(inquiry.TrySaveReply("The part is in the stock room today. Collect it at the counter.", "admin-2"));
        Assert.Equal("The part is in the stock room today. Collect it at the counter.", inquiry.AdminReply);
        Assert.Equal("admin-2", inquiry.AdminUserId);
        Assert.Equal(ProductInquiry.LabelAnswered, inquiry.StatusLabel);
    }

    [Fact]
    public void Rejected_edit_leaves_the_original_reply_in_place()
    {
        var inquiry = Answered("Original reply.");

        Assert.Equal("Reply cannot be empty.", inquiry.TrySaveReply("   ", "admin-1"));
        Assert.Equal("Original reply.", inquiry.AdminReply);

        Assert.Equal("Reply cannot exceed 2 000 characters.", inquiry.TrySaveReply(new string('x', 2001), "admin-1"));
        Assert.Equal("Original reply.", inquiry.AdminReply);
        Assert.Equal("support-1", inquiry.AdminUserId);
    }

    [Fact]
    public void Close_and_reopen_keep_the_saved_reply()
    {
        var inquiry = Answered("We have one left.");
        var repliedAt = inquiry.AdminRepliedAt;

        inquiry.MarkClosed();
        Assert.Equal("We have one left.", inquiry.AdminReply);
        Assert.Equal(ProductInquiry.LabelClosed, inquiry.StatusLabel);
        Assert.Equal(repliedAt, inquiry.AdminRepliedAt);

        inquiry.MarkReopened();
        Assert.Equal("We have one left.", inquiry.AdminReply);
        Assert.Equal(ProductInquiry.LabelAwaitingFollowUp, inquiry.StatusLabel);
        Assert.Equal("support-1", inquiry.AdminUserId);
        Assert.Equal(repliedAt, inquiry.AdminRepliedAt);
    }

    [Fact]
    public void Member_follow_up_keeps_the_replaced_reply()
    {
        var inquiry = Answered("First answer.");
        Assert.Null(inquiry.TrySaveReply("Updated answer.", "support-1"));
        inquiry.MarkClosed();

        Assert.Null(inquiry.TryAppendFollowUp("Can I collect it tomorrow?"));
        Assert.Equal("Updated answer.", inquiry.AdminReply);
        Assert.Contains("Can I collect it tomorrow?", inquiry.Message, StringComparison.Ordinal);
        Assert.Equal(ProductInquiry.LabelAwaitingFollowUp, inquiry.StatusLabel);
    }

    [Fact]
    public void Admin_and_member_screens_label_the_reply_as_AutoVault_Support()
    {
        var root = FindRepositoryRoot();
        var admin = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminInquiries", "Index.cshtml"));
        var member = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Account", "ProductQuestions.cshtml"));
        var service = File.ReadAllText(Path.Combine(root, "AutoVault.Service", "MVC", "Services", "ShopCatalogService.cs"));

        Assert.Contains("AutoVault Support", admin, StringComparison.Ordinal);
        Assert.Contains("inq-reply-original-", admin, StringComparison.Ordinal);
        Assert.Contains("AutoVault Support", member, StringComparison.Ordinal);
        Assert.Contains("TrySaveReply", service, StringComparison.Ordinal);
        Assert.DoesNotContain("TryAppendReply", service, StringComparison.Ordinal);
        Assert.Contains("AutoVault Support:", service, StringComparison.Ordinal);
    }

    private static ProductInquiry Answered(string reply)
    {
        var inquiry = new ProductInquiry
        {
            Message = "Is this part available?",
            CustomerName = "Member",
            Email = "member@example.com"
        };
        Assert.Null(inquiry.TrySaveReply(reply, "support-1"));
        return inquiry;
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FinalProject.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Could not find FinalProject.slnx.");
    }
}
