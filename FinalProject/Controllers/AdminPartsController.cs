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
    public async Task<ActionResult<PartAdminListResponse>> List(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PartsAdminService.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _parts.ListAsync(search, category, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PartAdminItem>> Get(int id, CancellationToken cancellationToken)
    {
        var part = await _parts.GetAsync(id, cancellationToken);
        return part == null ? NotFound(new { message = "The part was not found." }) : Ok(part);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<PartAdminItem>> Create(
        [FromBody] SavePartRequest request,
        CancellationToken cancellationToken)
    {
        var (part, error) = await _parts.CreateAsync(request, cancellationToken);
        return part == null ? BadRequest(new { message = error }) : Ok(part);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PartAdminItem>> Update(
        int id,
        [FromBody] SavePartRequest request,
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
    public async Task<ActionResult<PartDeleteResponse>> Delete(int id, CancellationToken cancellationToken)
    {
        var (found, outcome, message) = await _parts.DeleteAsync(id, cancellationToken);
        if (!found)
        {
            return NotFound(new { message });
        }

        return Ok(new PartDeleteResponse { Outcome = outcome, Message = message });
    }
}
