using FinalProject.Commerce;
using FinalProject.DTOs;
using FinalProject.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.Controllers;

[ApiController]
[Route("api/cars/explore")]
public sealed class CarDiscoveryController : ControllerBase
{
    private readonly ICarDiscoveryService _discovery;

    public CarDiscoveryController(ICarDiscoveryService discovery)
    {
        _discovery = discovery;
    }

    [HttpGet("most-viewed")]
    [ProducesResponseType(typeof(PagedResultDto<RankedCarResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResultDto<RankedCarResponseDto>>> MostViewed(
        [FromQuery] string? period,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = CarDiscoveryRules.DefaultPageSize,
        [FromQuery] bool usedOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (!CarDiscoveryRules.TryNormalizePeriod(period, out var normalized))
        {
            return BadRequest(new { message = "Period must be today, week, or all." });
        }

        return Ok(await _discovery.GetMostViewedAsync(
            normalized,
            page,
            pageSize,
            usedOnly: usedOnly,
            cancellationToken: cancellationToken));
    }

    [HttpGet("most-favorited")]
    [ProducesResponseType(typeof(PagedResultDto<RankedCarResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<RankedCarResponseDto>>> MostFavorited(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = CarDiscoveryRules.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _discovery.GetMostFavoritedAsync(page, pageSize, cancellationToken));
    }

    [HttpGet("latest")]
    [ProducesResponseType(typeof(PagedResultDto<CarResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<CarResponseDto>>> Latest(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = CarDiscoveryRules.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _discovery.GetLatestArrivalsAsync(page, pageSize, cancellationToken));
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(CarDiscoveryStatisticsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CarDiscoveryStatisticsResponseDto>> Statistics(CancellationToken cancellationToken)
    {
        return Ok(await _discovery.GetStatisticsAsync(cancellationToken: cancellationToken));
    }
}
