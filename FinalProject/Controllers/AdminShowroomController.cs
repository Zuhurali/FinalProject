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

    [HttpGet("sales")]
    [ProducesResponseType(typeof(IReadOnlyList<ShowroomSaleItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ShowroomSaleItemDto>>> ListSales(CancellationToken cancellationToken)
    {
        return Ok(await _showroom.ListSalesAsync(cancellationToken));
    }

    [HttpPost("sales")]
    [ProducesResponseType(typeof(ShowroomSaleItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
