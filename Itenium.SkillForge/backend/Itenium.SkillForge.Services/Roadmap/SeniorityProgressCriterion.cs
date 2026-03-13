namespace Itenium.SkillForge.Services.Roadmap;

/// <summary>A single unmet threshold criterion for the next seniority level.</summary>
public sealed record SeniorityProgressCriterion(
    int SkillId,
    string SkillName,
    int MinNiveau,
    int CurrentNiveau);
