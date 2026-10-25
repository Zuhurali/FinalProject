using System.Security.Claims;
using FinalProject.Models;
using FinalProject.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductReviewsController : ControllerBase
{
    private readonly IProductReviewService _reviews;

    public ProductReviewsController(IProductReviewService reviews)
    {
        _reviews = reviews;
    }

    [AllowAnonymous]
    [HttpGet("{productId:int}/reviews")]
    public async Task<ActionResult<IReadOnlyList<ProductReviewItem>>> Get(
        int productId,
        CancellationToken cancellationToken)
    {
        var includeHidden = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
        var result = await _reviews.GetReviewsAsync(
            productId,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            includeHidden,
            cancellationToken);
        return result == null ? NotFound(new { message = "Product not found." }) : Ok(result);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet("reviews/moderation")]
    public async Task<ActionResult<IReadOnlyList<ProductReviewItem>>> Moderate(
        CancellationToken cancellationToken)
    {
        return Ok(await _reviews.GetAllForModerationAsync(cancellationToken));
    }

    [Authorize]
    [HttpPost("{productId:int}/reviews")]
    public async Task<ActionResult<ProductReviewItem>> Create(
        int productId,
        SaveProductReviewRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var (review, error) = await _reviews.SaveReviewAsync(productId, null, userId, request.Content, cancellationToken);
        return review == null ? BadRequest(new { message = error }) : Ok(review);
    }

    [Authorize]
    [HttpPut("{productId:int}/reviews/{reviewId:int}")]
    public async Task<ActionResult<ProductReviewItem>> Update(
        int productId,
        int reviewId,
        SaveProductReviewRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var (review, error) = await _reviews.SaveReviewAsync(productId, reviewId, userId, request.Content, cancellationToken);
        if (review != null)
        {
            return Ok(review);
        }

        return error == "Review not found."
            ? NotFound(new { message = error })
            : BadRequest(new { message = error });
    }

    [Authorize]
    [HttpDelete("reviews/{reviewId:int}")]
    public async Task<IActionResult> Delete(int reviewId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var (ok, error) = await _reviews.DeleteReviewAsync(reviewId, userId, cancellationToken);
        if (ok)
        {
            return NoContent();
        }

        return error == "Review not found." ? NotFound(new { message = error }) : Forbid();
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpDelete("reviews/{reviewId:int}/admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminDelete(int reviewId, CancellationToken cancellationToken)
    {
        var deleted = await _reviews.DeleteReviewAsAdminAsync(reviewId, cancellationToken);
        return deleted ? NoContent() : NotFound(new { message = "Review not found." });
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet("reviews/{reviewId:int}/history")]
    public async Task<ActionResult<IReadOnlyList<ProductReviewRevisionItem>>> History(
        int reviewId,
        CancellationToken cancellationToken)
    {
        var history = await _reviews.GetHistoryAsync(reviewId, cancellationToken);
        return history == null ? NotFound(new { message = "Review not found." }) : Ok(history);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost("reviews/{reviewId:int}/visibility")]
    public async Task<IActionResult> SetVisibility(
        int reviewId,
        [FromQuery] bool hidden,
        CancellationToken cancellationToken)
    {
        var updated = await _reviews.SetHiddenAsync(reviewId, hidden, cancellationToken);
        return updated ? NoContent() : NotFound(new { message = "Review not found." });
    }
}
