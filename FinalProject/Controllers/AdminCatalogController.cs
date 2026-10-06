using FinalProject.DTOs;
using FinalProject.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class AdminCatalogController : ControllerBase
{
    private readonly ICatalogAdminService _catalogAdmin;

    public AdminCatalogController(ICatalogAdminService catalogAdmin)
    {
        _catalogAdmin = catalogAdmin;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AdminSummaryResponse>> Summary(CancellationToken cancellationToken)
    {
        return Ok(await _catalogAdmin.GetSummaryAsync(cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("brands")]
    public async Task<ActionResult<BrandResponse>> CreateBrand(
        [FromBody] SaveLookupRequest request,
        CancellationToken cancellationToken)
    {
        var brand = await _catalogAdmin.CreateBrandAsync(request.Name, cancellationToken);
        return brand == null ? BadRequest(new { message = "Enter a unique brand name." }) : Ok(brand);
    }

    [HttpPut("brands/{id:int}")]
    public async Task<ActionResult<BrandResponse>> UpdateBrand(
        int id,
        [FromBody] SaveLookupRequest request,
        CancellationToken cancellationToken)
    {
        var brand = await _catalogAdmin.UpdateBrandAsync(id, request.Name, cancellationToken);
        return brand == null ? BadRequest(new { message = "The brand could not be updated." }) : Ok(brand);
    }

    [HttpDelete("brands/{id:int}")]
    public async Task<IActionResult> DeleteBrand(int id, CancellationToken cancellationToken)
    {
        return await _catalogAdmin.DeleteBrandAsync(id, cancellationToken)
            ? NoContent()
            : BadRequest(new { message = "The brand is in use or was not found." });
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("categories")]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(
        [FromBody] SaveLookupRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _catalogAdmin.CreateCategoryAsync(request.Name, cancellationToken);
        return category == null ? BadRequest(new { message = "Enter a unique category name." }) : Ok(category);
    }

    [HttpPut("categories/{id:int}")]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(
        int id,
        [FromBody] SaveLookupRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _catalogAdmin.UpdateCategoryAsync(id, request.Name, cancellationToken);
        return category == null ? BadRequest(new { message = "The category could not be updated." }) : Ok(category);
    }

    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken)
    {
        return await _catalogAdmin.DeleteCategoryAsync(id, cancellationToken)
            ? NoContent()
            : BadRequest(new { message = "The category is in use or was not found." });
    }
}
