namespace Itenium.SkillForge.Services.Roadmap;

/// <summary>An unmet prerequisite on a skill node in the consultant's roadmap.</summary>
public sealed record SkillPrerequisiteWarning(
    int RequiredSkillId,
    string RequiredSkillName,
    int RequiredMinNiveau,
    int CurrentNiveau);
