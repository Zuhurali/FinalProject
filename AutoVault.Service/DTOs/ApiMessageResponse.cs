namespace FinalProject.DTOs;

/// <summary>
/// Documents a JSON error or status body of <c>{ "message": "..." }</c>.
/// Actions still return anonymous objects; this type is used only by OpenAPI.
/// </summary>
public sealed class ApiMessageResponse
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Documents the conflict body returned when a product rating already exists.
/// </summary>
public sealed class ProductRatingConflictResponse
{
    public string Message { get; set; } = string.Empty;

    public int ExistingRatingId { get; set; }

    public int ExistingRating { get; set; }
}

/// <summary>
/// Documents <c>{ "hidden": true }</c> from a successful conversation hide.
/// </summary>
public sealed class ConversationHiddenResponse
{
    public bool Hidden { get; set; }
}
