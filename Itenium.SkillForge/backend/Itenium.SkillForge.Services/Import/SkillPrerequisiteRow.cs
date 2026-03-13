namespace Itenium.SkillForge.Services.Import;

/// <summary>CSV row representing a prerequisite link between two skills.</summary>
public sealed record SkillPrerequisiteRow(string SkillName, string RequiredSkillName, int RequiredMinNiveau);
