using System.ComponentModel.DataAnnotations;
using Itenium.SkillForge.Entities.Skills;

namespace Itenium.SkillForge.Entities.Resources;

public class ResourceEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public required string Title { get; set; }

    [Required]
    [MaxLength(2000)]
    public required string Url { get; set; }

    public ResourceType Type { get; set; }

    public int SkillId { get; set; }

    public SkillEntity Skill { get; set; } = null!;

    /// <summary>Minimum niveau this resource is relevant for.</summary>
    public int FromNiveau { get; set; }

    /// <summary>Maximum niveau this resource is relevant for.</summary>
    public int ToNiveau { get; set; }

    /// <summary>FK to the identity user who contributed this resource.</summary>
    [Required]
    [MaxLength(EntityConstants.UserIdMaxLength)]
    public required string AddedByUserId { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ResourceCompletionEntity> Completions { get; set; } = [];

    public ICollection<ResourceRatingEntity> Ratings { get; set; } = [];

    public override string ToString() => Title;
}
