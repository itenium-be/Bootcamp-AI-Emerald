using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities.Coaching;

public class CoachingSessionEntity
{
    [Key]
    public int Id { get; set; }

    /// <summary>FK to the identity user who is the consultant in this session.</summary>
    [Required]
    [MaxLength(EntityConstants.UserIdMaxLength)]
    public required string ConsultantUserId { get; set; }

    /// <summary>FK to the identity user who is the coach running this session.</summary>
    [Required]
    [MaxLength(EntityConstants.UserIdMaxLength)]
    public required string CoachUserId { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAt { get; set; }

    [MaxLength(5000)]
    public string? Notes { get; set; }

    public ICollection<SkillValidationEntity> Validations { get; set; } = [];
}
