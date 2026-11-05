using FinalProject.Commerce;
using FinalProject.DTOs;
using FinalProject.Security;
using FinalProject.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace FinalProject.Controllers;

[ApiController]
[Route("api/cars")]
public class CarsController : ControllerBase
{
    private readonly ICarService _carService;
    private readonly ICarDiscoveryService _discovery;

    public CarsController(ICarService carService, ICarDiscoveryService discovery)
    {
        _carService = carService;
        _discovery = discovery;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<CarResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResultDto<CarResponseDto>>> GetAll([FromQuery] CarListQueryDto query, CancellationToken cancellationToken)
    {
        var validationError = query.Validate();
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        // Anonymous / member callers may list Active (default) or Sold. Draft stays admin/owner-only.
        if (!User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
        {
            query.Status = CarPublicListing.NormalizeAnonymousListStatus(query.Status);
        }

        var cars = await _carService.QueryAsync(query, cancellationToken);
        return Ok(cars);
    }

    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(IReadOnlyList<CarSuggestionResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CarSuggestionResponseDto>>> GetSuggestions([FromQuery] string? query, [FromQuery] int limit = 8, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return Ok(Array.Empty<CarSuggestionResponseDto>());
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
    [ProducesResponseType(typeof(CarResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarResponseDto>> GetById(int id, CancellationToken cancellationToken)
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

        // Public callers only see Active cars. Owners and Admin/SuperAdmin may load Draft/Sold for management.
        if (!CarPublicListing.IsPubliclyListed(car.Status) && !CanManage(car.OwnerId))
        {
            return NotFound();
        }

        return Ok(car);
    }

    /// <summary>Anonymous view counter. Rate-limited; login is not required.</summary>
    [EnableRateLimiting(CarViewsRateLimit.PolicyName)]
    [HttpPost("{id:int}/views")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RecordView(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Car id must be greater than zero." });
        }

        return await _discovery.RecordViewAsync(id, cancellationToken: cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [AllowAnonymous]
    [HttpGet("lookups")]
    [ProducesResponseType(typeof(CarLookupResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CarLookupResponseDto>> GetLookups(CancellationToken cancellationToken)
    {
        return Ok(await _carService.GetLookupsAsync(cancellationToken));
    }

    [Authorize]
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<CarResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<CarResponseDto>>> GetMine(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        return userId == null
            ? Unauthorized()
            : Ok(await _carService.GetOwnedAsync(userId, cancellationToken));
    }

    /// <summary>
    /// SuperAdmin-only listing of cars owned by a specific member (Member Details).
    /// </summary>
    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("by-owner/{ownerId}")]
    [ProducesResponseType(typeof(IReadOnlyList<CarResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<CarResponseDto>>> GetByOwner(
        string ownerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return BadRequest(new { message = "Owner id is required." });
        }

        return Ok(await _carService.GetOwnedAsync(ownerId.Trim(), cancellationToken));
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    [ProducesResponseType(typeof(CarResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CarResponseDto>> Create(
        [FromBody] SaveCarRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            var car = await _carService.CreateAsync(userId, request, cancellationToken);
            return car == null
                ? BadRequest(new { message = "The owner, brand, or category is invalid." })
                : CreatedAtAction(nameof(GetById), new { id = car.Id }, car);
        }
        catch (CarGalleryException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (CarListingException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost("catalog")]
    [ProducesResponseType(typeof(CarResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CarResponseDto>> CreateCatalog(
        [FromBody] SaveCarRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var car = await _carService.CreateCatalogAsync(request, cancellationToken);
            return car == null
                ? BadRequest(new { message = "The brand or category is invalid." })
                : CreatedAtAction(nameof(GetById), new { id = car.Id }, car);
        }
        catch (CarGalleryException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (CarListingException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [Authorize]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CarResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarResponseDto>> Update(
        int id,
        [FromBody] SaveCarRequestDto request,
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

        try
        {
            var updated = await _carService.UpdateAsync(id, request, cancellationToken);
            return updated == null
                ? BadRequest(new { message = "The brand or category is invalid." })
                : Ok(updated);
        }
        catch (CarGalleryException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (CarListingException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
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
