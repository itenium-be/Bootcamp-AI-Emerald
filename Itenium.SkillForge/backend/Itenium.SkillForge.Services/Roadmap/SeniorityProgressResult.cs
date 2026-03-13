using Itenium.SkillForge.Entities.Profiles;

namespace Itenium.SkillForge.Services.Roadmap;

/// <summary>
/// The consultant's progress toward their next seniority level.
/// E.g. "You meet 14/18 Medior requirements".
/// </summary>
public sealed record SeniorityProgressResult(
    /// <summary>The highest fully-achieved level; null when not yet Junior.</summary>
    SeniorityLevel? CurrentLevel,
    /// <summary>The next level to aim for; null when already Senior.</summary>
    SeniorityLevel? NextLevel,
    int Met,
    int Required,
    IReadOnlyList<SeniorityProgressCriterion> UnmetCriteria);
