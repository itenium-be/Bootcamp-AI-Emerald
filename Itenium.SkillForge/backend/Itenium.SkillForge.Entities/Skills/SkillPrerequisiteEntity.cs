namespace Itenium.SkillForge.Entities.Skills;

/// <summary>
/// Defines that a skill requires another skill to be at a minimum niveau before it is recommended.
/// Prerequisites are non-blocking: the consultant sees a warning but is never locked out.
/// Composite PK: (SkillId, RequiredSkillId) — configured via Fluent API in AppDbContext.
/// </summary>
public class SkillPrerequisiteEntity
{
    public int SkillId { get; set; }

    public SkillEntity Skill { get; set; } = null!;

    public int RequiredSkillId { get; set; }

    public SkillEntity RequiredSkill { get; set; } = null!;

    /// <summary>
    /// Minimum niveau the consultant must have validated on RequiredSkill.
    /// </summary>
    public int RequiredMinNiveau { get; set; }
}
