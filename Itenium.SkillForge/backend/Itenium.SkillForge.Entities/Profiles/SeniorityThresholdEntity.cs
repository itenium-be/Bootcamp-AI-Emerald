using System.ComponentModel.DataAnnotations;
using Itenium.SkillForge.Entities.Skills;

namespace Itenium.SkillForge.Entities.Profiles;

/// <summary>
/// Defines the minimum skill niveau a consultant must have validated to meet a seniority level.
/// Static ruleset per profile. Computed at read time — no background jobs.
/// </summary>
public class SeniorityThresholdEntity
{
    [Key]
    public int Id { get; set; }

    public int ProfileId { get; set; }

    public CompetenceCentreProfileEntity Profile { get; set; } = null!;

    public SeniorityLevel SeniorityLevel { get; set; }

    public int SkillId { get; set; }

    public SkillEntity Skill { get; set; } = null!;

    public int MinNiveau { get; set; }
}
