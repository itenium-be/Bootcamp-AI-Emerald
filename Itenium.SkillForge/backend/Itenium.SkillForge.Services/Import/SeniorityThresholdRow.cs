using Itenium.SkillForge.Entities.Profiles;

namespace Itenium.SkillForge.Services.Import;

/// <summary>CSV row representing a seniority threshold for a profile skill.</summary>
public sealed record SeniorityThresholdRow(
    string ProfileName,
    SeniorityLevel SeniorityLevel,
    string SkillName,
    int MinNiveau);
