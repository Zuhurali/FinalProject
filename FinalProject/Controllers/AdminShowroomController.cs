using System.Security.Claims;
using FinalProject.DTOs;
using FinalProject.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.Controllers;

[ApiController]
[Route("api/admin/showroom")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class AdminShowroomController : ControllerBase
{
    private readonly IShowroomAdminService _showroom;

    public AdminShowroomController(IShowroomAdminService showroom)
    {
        _showroom = showroom;
    }

    [HttpGet("requests")]
    public async Task<ActionResult<IReadOnlyList<ShowroomRequestItemDto>>> ListRequests(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        return Ok(await _showroom.ListRequestsAsync(status, cancellationToken));
    }

    [HttpGet("requests/{id:int}")]
    public async Task<ActionResult<ShowroomRequestItemDto>> GetRequest(int id, CancellationToken cancellationToken)
    {
        var item = await _showroom.GetRequestAsync(id, cancellationToken);
        return item == null ? NotFound(new { message = "Request not found." }) : Ok(item);
    }

    [HttpPatch("requests/{id:int}/status")]
    public async Task<ActionResult<ShowroomRequestItemDto>> SetRequestStatus(
        int id,
        [FromBody] UpdateShowroomRequestStatusRequestDto body,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var (item, error) = await _showroom.SetRequestStatusAsync(id, body.Status, cancellationToken);
        if (item == null && error == "Request not found.")
        {
            return NotFound(new { message = error });
        }

        return item == null ? BadRequest(new { message = error }) : Ok(item);
    }

    [HttpDelete("requests/{id:int}")]
    public async Task<IActionResult> DeleteRequest(int id, CancellationToken cancellationToken)
    {
        var (found, error) = await _showroom.DeleteRequestAsync(id, cancellationToken);
        return found ? NoContent() : NotFound(new { message = error ?? "Request not found." });
    }

    [HttpPost("requests/{id:int}/complete-sale")]
    public async Task<ActionResult<ShowroomSaleItemDto>> CompleteRequestSale(
        int id,
        [FromBody] CompleteShowroomRequestSaleRequestDto? body,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var (sale, error) = await _showroom.CompleteRequestWithSaleAsync(
            id,
            userId,
            body?.Note,
            cancellationToken);
        if (sale == null && error == "Request not found.")
        {
            return NotFound(new { message = error });
        }

        return sale == null ? BadRequest(new { message = error }) : Ok(sale);
    }

    [HttpGet("sales")]
    public async Task<ActionResult<IReadOnlyList<ShowroomSaleItemDto>>> ListSales(CancellationToken cancellationToken)
    {
        return Ok(await _showroom.ListSalesAsync(cancellationToken));
    }

    [HttpPost("sales")]
    public async Task<ActionResult<ShowroomSaleItemDto>> RecordSale(
        [FromBody] RecordShowroomSaleRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var (sale, error) = await _showroom.RecordSaleAsync(request, userId, cancellationToken);
        return sale == null ? BadRequest(new { message = error }) : Ok(sale);
    }

    [HttpPost("sales/{id:int}/void")]
    public async Task<IActionResult> VoidSale(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var (ok, error) = await _showroom.VoidSaleAsync(id, userId, cancellationToken);
        return ok ? NoContent() : BadRequest(new { message = error });
    }
}
