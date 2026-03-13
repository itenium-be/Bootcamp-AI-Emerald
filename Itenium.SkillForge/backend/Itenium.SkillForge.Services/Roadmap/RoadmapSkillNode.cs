namespace Itenium.SkillForge.Services.Roadmap;

/// <summary>
/// A single skill node in the consultant's roadmap, enriched with progress
/// and prerequisite state.
/// </summary>
public sealed record RoadmapSkillNode(
    int SkillId,
    string SkillName,
    string CategoryName,
    int LevelCount,
    /// <summary>Highest validated niveau; 0 when never validated.</summary>
    int CurrentNiveau,
    /// <summary>Target from the consultant's active goal; null when no goal exists.</summary>
    int? TargetNiveau,
    /// <summary>True when all prerequisites are met (or the skill has none).</summary>
    bool PrerequisitesMet,
    IReadOnlyList<SkillPrerequisiteWarning> UnmetPrerequisites);
