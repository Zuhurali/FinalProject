using FinalProject.DTOs;
using FinalProject.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinalProject.Controllers;

[ApiController]
[Route("api/cars")]
public class CarsController : ControllerBase
{
    private readonly ICarService _carService;

    public CarsController(ICarService carService)
    {
        _carService = carService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CarResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<CarResponse>>> GetAll([FromQuery] CarListQuery query, CancellationToken cancellationToken)
    {
        var validationError = query.Validate();
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        var cars = await _carService.QueryAsync(query, cancellationToken);
        return Ok(cars);
    }

    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(IReadOnlyList<CarSuggestionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CarSuggestionResponse>>> GetSuggestions([FromQuery] string? query, [FromQuery] int limit = 8, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return Ok(Array.Empty<CarSuggestionResponse>());
        }

        if (limit < 1)
        {
            limit = 8;
        }

        if (limit > 20)
        {
            limit = 20;
        }

        var suggestions = await _carService.GetSuggestionsAsync(query, limit, cancellationToken);
        return Ok(suggestions);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CarResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest("Car id must be greater than zero.");
        }

        var car = await _carService.GetByIdAsync(id, cancellationToken);
        if (car is null)
        {
            return NotFound();
        }

        return Ok(car);
    }

    [AllowAnonymous]
    [HttpGet("lookups")]
    [ProducesResponseType(typeof(CarLookupResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CarLookupResponse>> GetLookups(CancellationToken cancellationToken)
    {
        return Ok(await _carService.GetLookupsAsync(cancellationToken));
    }

    [Authorize]
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<CarResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<CarResponse>>> GetMine(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        return userId == null
            ? Unauthorized()
            : Ok(await _carService.GetOwnedAsync(userId, cancellationToken));
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    [ProducesResponseType(typeof(CarResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CarResponse>> Create(
        [FromBody] SaveCarRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var car = await _carService.CreateAsync(userId, request, cancellationToken);
        return car == null
            ? BadRequest(new { message = "The owner, brand, or category is invalid." })
            : CreatedAtAction(nameof(GetById), new { id = car.Id }, car);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("catalog")]
    [ProducesResponseType(typeof(CarResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CarResponse>> CreateCatalog(
        [FromBody] SaveCarRequest request,
        CancellationToken cancellationToken)
    {
        var car = await _carService.CreateCatalogAsync(request, cancellationToken);
        return car == null
            ? BadRequest(new { message = "The brand or category is invalid." })
            : CreatedAtAction(nameof(GetById), new { id = car.Id }, car);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CarResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarResponse>> Update(
        int id,
        [FromBody] SaveCarRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _carService.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            return NotFound();
        }

        if (!CanManage(existing.OwnerId))
        {
            return Forbid();
        }

        var updated = await _carService.UpdateAsync(id, request, cancellationToken);
        return updated == null
            ? BadRequest(new { message = "The brand or category is invalid." })
            : Ok(updated);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var existing = await _carService.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            return NotFound();
        }

        if (!CanManage(existing.OwnerId))
        {
            return Forbid();
        }

        await _carService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private string? CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private bool CanManage(string? ownerId)
    {
        // Catalog cars have no owner. Admin and SuperAdmin may still edit or delete them.
        if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
        {
            return true;
        }

        var userId = CurrentUserId();
        return userId != null
            && !string.IsNullOrWhiteSpace(ownerId)
            && string.Equals(userId, ownerId, StringComparison.Ordinal);
    }
}
