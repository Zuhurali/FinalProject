using FinalProject.DTOs;
using FinalProject.Services;
using FinalProject.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.Controllers;

[ApiController]
[Route("api/admin/parts")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class AdminPartsController : ControllerBase
{
    private readonly IPartsAdminService _parts;

    public AdminPartsController(IPartsAdminService parts)
    {
        _parts = parts;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PartAdminListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PartAdminListResponseDto>> List(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? brand,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PartsAdminService.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _parts.ListAsync(search, category, brand, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PartAdminItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PartAdminItemDto>> Get(int id, CancellationToken cancellationToken)
    {
        var part = await _parts.GetAsync(id, cancellationToken);
        return part == null ? NotFound(new { message = "The part was not found." }) : Ok(part);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    [ProducesResponseType(typeof(PartAdminItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PartAdminItemDto>> Create(
        [FromBody] SavePartRequestDto request,
        CancellationToken cancellationToken)
    {
        var (part, error) = await _parts.CreateAsync(request, cancellationToken);
        return part == null ? BadRequest(new { message = error }) : Ok(part);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PartAdminItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PartAdminItemDto>> Update(
        int id,
        [FromBody] SavePartRequestDto request,
        CancellationToken cancellationToken)
    {
        var (part, error) = await _parts.UpdateAsync(id, request, cancellationToken);
        if (part == null && error == "The part was not found.")
        {
            return NotFound(new { message = error });
        }

        return part == null ? BadRequest(new { message = error }) : Ok(part);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(PartDeleteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PartDeleteResponseDto>> Delete(int id, CancellationToken cancellationToken)
    {
        var (found, outcome, message) = await _parts.DeleteAsync(id, cancellationToken);
        if (!found)
        {
            return NotFound(new { message });
        }

        return Ok(new PartDeleteResponseDto { Outcome = outcome, Message = message });
    }
}
