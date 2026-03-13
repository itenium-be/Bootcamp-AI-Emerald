using System.ComponentModel.DataAnnotations;
using Itenium.SkillForge.Entities.Goals;

namespace Itenium.SkillForge.Entities.Resources;

/// <summary>
/// Records that a consultant completed a resource. Serves as evidence toward a goal.
/// </summary>
public class ResourceCompletionEntity
{
    [Key]
    public int Id { get; set; }

    public int ResourceId { get; set; }

    public ResourceEntity Resource { get; set; } = null!;

    /// <summary>FK to the identity user who completed this resource.</summary>
    [Required]
    [MaxLength(EntityConstants.UserIdMaxLength)]
    public required string UserId { get; set; }

    /// <summary>Optional link to a goal, recording this completion as evidence.</summary>
    public int? GoalId { get; set; }

    public GoalEntity? Goal { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
