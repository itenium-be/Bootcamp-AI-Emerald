using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities.Resources;

/// <summary>
/// Thumbs up/down rating of a resource by a user.
/// Composite PK: (ResourceId, UserId) — one rating per user per resource, configured via Fluent API.
/// </summary>
public class ResourceRatingEntity
{
    public int ResourceId { get; set; }

    public ResourceEntity Resource { get; set; } = null!;

    /// <summary>FK to the identity user who rated this resource.</summary>
    [Required]
    [MaxLength(450)]
    public required string UserId { get; set; }

    public bool IsPositive { get; set; }
}
