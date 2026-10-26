using System.Security.Claims;
using FinalProject.Models;
using FinalProject.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductRatingsController : ControllerBase
{
    private readonly IProductRatingService _ratingService;

    public ProductRatingsController(IProductRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    [AllowAnonymous]
    [HttpGet("{productId:int}/ratings")]
    [ProducesResponseType<ProductRatingSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductRatingSummary>> GetRatings(
        int productId,
        CancellationToken cancellationToken)
    {
        var result = await _ratingService.GetProductRatingsAsync(productId, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = "Product not found." });
        }

        return User.IsInRole("Admin") || User.IsInRole("SuperAdmin")
            ? Ok(result)
            : Ok(result with { Ratings = Array.Empty<ProductRatingItem>() });
    }

    [AllowAnonymous]
    [HttpGet("ratings")]
    [ProducesResponseType<IReadOnlyList<ProductRatingSummaryItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductRatingSummaryItem>>> GetSummaries(
        [FromQuery] int[] productIds,
        CancellationToken cancellationToken)
    {
        var result = await _ratingService.GetSummariesAsync(productIds, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet("ratings/admin")]
    [ProducesResponseType<IReadOnlyList<ProductRatingItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ProductRatingItem>>> GetAllForAdmin(
        CancellationToken cancellationToken)
    {
        return Ok(await _ratingService.GetAllForAdminAsync(cancellationToken));
    }

    [Authorize]
    [HttpGet("{productId:int}/ratings/my")]
    [ProducesResponseType<CurrentUserProductRating>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrentUserProductRating>> GetMyRating(
        int productId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var product = await _ratingService.GetProductRatingsAsync(productId, cancellationToken);
        if (product == null)
        {
            return NotFound(new { message = "Product not found." });
        }

        var result = await _ratingService.GetUserRatingAsync(productId, userId, cancellationToken);
        return result == null ? NoContent() : Ok(result);
    }

    [Authorize]
    [HttpPost("{productId:int}/ratings")]
    [ProducesResponseType<CurrentUserProductRating>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CurrentUserProductRating>> SaveRating(
        int productId,
        [FromBody] SaveProductRatingRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || request.Rating is < 1 or > 5)
        {
            return BadRequest(new { message = "Rating must be between 1 and 5." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var product = await _ratingService.GetProductRatingsAsync(productId, cancellationToken);
        if (product == null)
        {
            return NotFound(new { message = "Product not found." });
        }

        var result = await _ratingService.SaveRatingAsync(
            productId,
            userId,
            request.Rating,
            cancellationToken);

        if (result == null)
        {
            return Unauthorized(new { message = "The authenticated user no longer exists." });
        }

        if (result.AlreadyExists)
        {
            return Conflict(new
            {
                message = "You already have a rating for this product. Delete your existing rating before submitting a new one.",
                existingRatingId = result.Id,
                existingRating = result.Rating
            });
        }

        return Ok(result);
    }

    /// <summary>Allows the authenticated user to delete their own rating.</summary>
    [Authorize]
    [HttpDelete("{productId:int}/ratings/{ratingId:int}/my")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMyRating(
        int productId,
        int ratingId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var (found, allowed) = await _ratingService.DeleteRatingByUserAsync(
            productId, ratingId, userId, cancellationToken);

        if (!found) return NotFound(new { message = "Rating not found." });
        if (!allowed) return Forbid();
        return NoContent();
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpDelete("{productId:int}/ratings/{ratingId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRating(
        int productId,
        int ratingId,
        CancellationToken cancellationToken)
    {
        var deleted = await _ratingService.DeleteRatingAsync(
            productId,
            ratingId,
            cancellationToken);
        return deleted ? NoContent() : NotFound(new { message = "Rating not found." });
    }
}
