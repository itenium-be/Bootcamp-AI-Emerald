namespace Itenium.SkillForge.Services.SkillCatalogue;

/// <summary>
/// Lightweight summary of a skill for browse/list views.
/// </summary>
public sealed record SkillListItem(
    int Id,
    string Name,
    string CategoryName,
    int LevelCount,
    string? Description);
