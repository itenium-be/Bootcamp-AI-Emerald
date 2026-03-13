using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities.Skills;

public class SkillLevelEntity
{
    [Key]
    public int Id { get; set; }

    public int SkillId { get; set; }

    public SkillEntity Skill { get; set; } = null!;

    /// <summary>
    /// The level number, 1-based. Must be within the skill's LevelCount.
    /// </summary>
    public int Niveau { get; set; }

    [Required]
    [MaxLength(1000)]
    public required string Descriptor { get; set; }

    public override string ToString() => $"Niveau {Niveau}: {Descriptor}";
}
