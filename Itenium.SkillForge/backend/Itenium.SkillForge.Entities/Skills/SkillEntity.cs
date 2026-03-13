using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities.Skills;

public class SkillEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    public int CategoryId { get; set; }

    public SkillCategoryEntity Category { get; set; } = null!;

    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Number of progression levels for this skill. 1 = checkbox (done/not done), 2–7 = progression.
    /// </summary>
    public int LevelCount { get; set; } = 1;

    public ICollection<SkillLevelEntity> Levels { get; set; } = [];

    public ICollection<SkillPrerequisiteEntity> Prerequisites { get; set; } = [];

    public override string ToString() => Name;
}
