namespace Itenium.SkillForge.Services.Import;

/// <summary>CSV row representing a skill.</summary>
public sealed record SkillRow(string Name, string Category, int LevelCount, string? Description);
