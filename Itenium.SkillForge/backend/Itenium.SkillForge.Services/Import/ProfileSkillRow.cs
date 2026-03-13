namespace Itenium.SkillForge.Services.Import;

/// <summary>CSV row assigning a skill to a competence centre profile.</summary>
public sealed record ProfileSkillRow(string ProfileName, string SkillName);
