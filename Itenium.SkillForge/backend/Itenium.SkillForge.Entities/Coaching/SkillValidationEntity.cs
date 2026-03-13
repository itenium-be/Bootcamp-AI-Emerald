using System.ComponentModel.DataAnnotations;
using Itenium.SkillForge.Entities.Skills;

namespace Itenium.SkillForge.Entities.Coaching;

/// <summary>
/// An immutable record of a coach validating a skill niveau for a consultant.
/// CoachUserId and ValidatedAt must never be updated after creation — enforced at the API layer.
/// </summary>
public class SkillValidationEntity
{
    [Key]
    public int Id { get; set; }

    /// <summary>FK to the identity user who is the consultant.</summary>
    [Required]
    [MaxLength(450)]
    public required string ConsultantUserId { get; set; }

    /// <summary>FK to the identity user who performed the validation. Immutable after creation.</summary>
    [Required]
    [MaxLength(450)]
    public required string CoachUserId { get; set; }

    public int SkillId { get; set; }

    public SkillEntity Skill { get; set; } = null!;

    public int Niveau { get; set; }

    /// <summary>Server-set timestamp. Immutable after creation.</summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Optional link to the coaching session in which this validation occurred.</summary>
    public int? SessionId { get; set; }

    public CoachingSessionEntity? Session { get; set; }
}
