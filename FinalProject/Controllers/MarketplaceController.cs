using System.Security.Claims;
using FinalProject.DTOs;
using FinalProject.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.Controllers;

[ApiController]
[Authorize]
public sealed class MarketplaceController : ControllerBase
{
    private readonly IMarketplaceService _marketplace;

    public MarketplaceController(IMarketplaceService marketplace)
    {
        _marketplace = marketplace;
    }

    [HttpGet("api/account/summary")]
    public async Task<ActionResult<AccountSummaryResponse>> GetAccountSummary(
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        return userId == null
            ? Unauthorized()
            : Ok(await _marketplace.GetSummaryAsync(userId, cancellationToken));
    }

    [HttpGet("api/favorites")]
    public async Task<ActionResult<IReadOnlyList<FavoriteResponse>>> GetFavorites(
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        return userId == null
            ? Unauthorized()
            : Ok(await _marketplace.GetFavoritesAsync(userId, cancellationToken));
    }

    [HttpGet("api/favorites/ids")]
    public async Task<ActionResult<IReadOnlyList<int>>> GetFavoriteIds(
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        return userId == null
            ? Unauthorized()
            : Ok(await _marketplace.GetFavoriteIdsAsync(userId, cancellationToken));
    }

    [HttpPost("api/cars/{carId:int}/favorite")]
    public async Task<ActionResult<FavoriteResponse>> AddFavorite(
        int carId,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var favorite = await _marketplace.AddFavoriteAsync(userId, carId, cancellationToken);
        return favorite == null
            ? NotFound(new { message = "Car or user not found." })
            : Ok(favorite);
    }

    [HttpDelete("api/cars/{carId:int}/favorite")]
    public async Task<IActionResult> RemoveFavorite(
        int carId,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        await _marketplace.RemoveFavoriteAsync(userId, carId, cancellationToken);
        return NoContent();
    }

    [HttpGet("api/conversations")]
    public async Task<ActionResult<IReadOnlyList<ConversationSummaryResponse>>> GetConversations(
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        return userId == null
            ? Unauthorized()
            : Ok(await _marketplace.GetConversationsAsync(
                userId,
                asStoreStaff: IsStoreStaff(),
                cancellationToken));
    }

    [HttpGet("api/conversations/{id:int}")]
    public async Task<ActionResult<ConversationDetailsResponse>> GetConversation(
        int id,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var conversation = await _marketplace.GetConversationAsync(
            id,
            userId,
            markRead: true,
            asStoreStaff: IsStoreStaff(),
            cancellationToken);
        return conversation == null ? NotFound() : Ok(conversation);
    }

    [HttpPost("api/cars/{carId:int}/conversations")]
    public async Task<ActionResult<ConversationDetailsResponse>> StartConversation(
        int carId,
        [FromBody] StartConversationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var conversation = await _marketplace.StartConversationAsync(
            carId,
            userId,
            request,
            cancellationToken);
        return conversation == null
            ? BadRequest(new
            {
                message = "The seller is unavailable or you cannot message your own listing."
            })
            : Ok(conversation);
    }

    [HttpPost("api/conversations/{id:int}/messages")]
    public async Task<ActionResult<MessageResponse>> Reply(
        int id,
        [FromBody] ReplyMessageRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var message = await _marketplace.ReplyAsync(
            id,
            userId,
            request.Content,
            asStoreStaff: IsStoreStaff(),
            cancellationToken);
        return message == null ? NotFound() : Ok(message);
    }

    [HttpPost("api/conversations/{id:int}/hide")]
    public async Task<IActionResult> HideConversation(
        int id,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var hidden = await _marketplace.HideConversationAsync(
            id,
            userId,
            asStoreStaff: IsStoreStaff(),
            cancellationToken);
        return hidden
            ? Ok(new { hidden = true })
            : NotFound(new { message = "Conversation not found or you cannot hide it." });
    }

    private string? CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private bool IsStoreStaff() =>
        User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
}
