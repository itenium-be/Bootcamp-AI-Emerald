using System.ComponentModel.DataAnnotations;
using Itenium.SkillForge.Entities.Skills;

namespace Itenium.SkillForge.Entities;

public class ConsultantSkillEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public required string ConsultantId { get; set; }

    public int SkillId { get; set; }
    public SkillEntity Skill { get; set; } = null!;

    public int CurrentLevel { get; set; }

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
