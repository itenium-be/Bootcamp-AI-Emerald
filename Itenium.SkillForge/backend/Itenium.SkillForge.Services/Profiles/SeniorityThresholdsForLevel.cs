using Itenium.SkillForge.Entities.Profiles;

namespace Itenium.SkillForge.Services.Profiles;

/// <summary>
/// All skill thresholds required to reach a specific seniority level within a profile.
/// </summary>
public sealed record SeniorityThresholdsForLevel(
    SeniorityLevel Level,
    IReadOnlyList<SeniorityThresholdDto> Thresholds);
