using System.ComponentModel.DataAnnotations;
using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Entities.Resources;
using Itenium.SkillForge.Entities.Skills;

namespace Itenium.SkillForge.Entities.Goals;

public class GoalEntity
{
    [Key]
    public int Id { get; set; }

    /// <summary>FK to the identity user who is the consultant.</summary>
    [Required]
    [MaxLength(450)]
    public required string ConsultantUserId { get; set; }

    /// <summary>FK to the identity user who is the coach that set this goal.</summary>
    [Required]
    [MaxLength(450)]
    public required string CoachUserId { get; set; }

    public int SkillId { get; set; }

    public SkillEntity Skill { get; set; } = null!;

    public int CurrentNiveau { get; set; }

    public int TargetNiveau { get; set; }

    public DateTime Deadline { get; set; }

    public GoalStatus Status { get; set; } = GoalStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GoalResourceEntity> GoalResources { get; set; } = [];

    public ReadinessFlagEntity? ReadinessFlag { get; set; }
}
