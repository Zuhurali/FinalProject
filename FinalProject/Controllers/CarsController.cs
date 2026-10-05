using FinalProject.DTOs;
using FinalProject.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
}
