namespace Itenium.SkillForge.Services.Profiles;

/// <summary>
/// A single skill-threshold entry within a seniority level requirement.
/// </summary>
public sealed record SeniorityThresholdDto(int SkillId, string SkillName, int MinNiveau);
