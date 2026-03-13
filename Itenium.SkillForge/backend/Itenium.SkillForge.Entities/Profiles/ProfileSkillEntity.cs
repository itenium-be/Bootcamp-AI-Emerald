using Itenium.SkillForge.Entities.Skills;

namespace Itenium.SkillForge.Entities.Profiles;

/// <summary>
/// Join entity linking a skill to a competence centre profile.
/// Composite PK: (ProfileId, SkillId) — configured via Fluent API in AppDbContext.
/// </summary>
public class ProfileSkillEntity
{
    public int ProfileId { get; set; }

    public CompetenceCentreProfileEntity Profile { get; set; } = null!;

    public int SkillId { get; set; }

    public SkillEntity Skill { get; set; } = null!;
}
