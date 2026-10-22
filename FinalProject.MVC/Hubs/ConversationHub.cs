using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace FinalProject.MVC.Hubs;

/// <summary>
/// Real-time delivery for one car conversation. Connections join a group only
/// after the existing marketplace API confirms the caller may read that thread.
/// </summary>
[Authorize]
public sealed class ConversationHub : Hub
{
    public const string MessageReceivedEvent = "messageReceived";
    public const string ConversationRestoredEvent = "conversationRestored";
    public const string NotificationReceivedEvent = "notificationReceived";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMarketplaceApiService _marketplace;
    private readonly IApiAccessTokenService _tokens;

    public ConversationHub(
        UserManager<ApplicationUser> userManager,
        IMarketplaceApiService marketplace,
        IApiAccessTokenService tokens)
    {
        _userManager = userManager;
        _marketplace = marketplace;
        _tokens = tokens;
    }

    public static string GroupName(int conversationId) => $"conversation-{conversationId}";

    public static string InboxGroupName(string userId) => $"inbox-{userId}";

    public async Task JoinConversation(int conversationId)
    {
        var user = await _userManager.GetUserAsync(Context.User!);
        if (user == null)
        {
            throw new HubException("Sign in required.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var conversation = await _marketplace.GetConversationAsync(
            conversationId,
            _tokens.CreateToken(Context.User!, roles),
            Context.ConnectionAborted);
        if (conversation == null)
        {
            throw new HubException("You cannot join this conversation.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    public async Task JoinInbox()
    {
        var user = await _userManager.GetUserAsync(Context.User!);
        if (user == null)
        {
            throw new HubException("Sign in required.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, InboxGroupName(user.Id));
    }
}
