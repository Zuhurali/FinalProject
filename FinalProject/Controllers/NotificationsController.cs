using System.Security.Claims;
using FinalProject.DTOs;
using FinalProject.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<ActionResult<NotificationListResponseDto>> Get(
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId();
        return userId == null
            ? Unauthorized()
            : Ok(await _notifications.GetForUserAsync(userId, take, cancellationToken));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<NotificationUnreadCountsResponseDto>> GetUnreadCount(
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        return Ok(await _notifications.GetUnreadCountsAsync(userId, cancellationToken));
    }

    [HttpPost("{id:int}/read")]
    public async Task<ActionResult<NotificationUnreadCountsResponseDto>> MarkRead(
        int id,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var ok = await _notifications.MarkReadAsync(id, userId, cancellationToken);
        if (!ok)
        {
            return NotFound();
        }

        return Ok(await _notifications.GetUnreadCountsAsync(userId, cancellationToken));
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<NotificationUnreadCountsResponseDto>> MarkAllRead(
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        await _notifications.MarkAllReadAsync(userId, cancellationToken);
        return Ok(await _notifications.GetUnreadCountsAsync(userId, cancellationToken));
    }

    private string? CurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);
}
