namespace Itenium.SkillForge.Services.Import;

/// <summary>CSV row representing a skill level descriptor.</summary>
public sealed record SkillLevelRow(string SkillName, int Niveau, string Descriptor);
