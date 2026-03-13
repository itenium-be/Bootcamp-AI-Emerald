namespace Itenium.SkillForge.Services.SkillCatalogue;

/// <summary>
/// Full detail of a skill including level descriptors and prerequisites.
/// Returned by GET /api/skills/{id}.
/// </summary>
public sealed record SkillDetail(
    int Id,
    string Name,
    string CategoryName,
    int LevelCount,
    string? Description,
    IReadOnlyList<SkillLevelDto> Levels,
    IReadOnlyList<SkillPrerequisiteDto> Prerequisites);

public sealed record SkillLevelDto(int Niveau, string Descriptor);

public sealed record SkillPrerequisiteDto(
    int RequiredSkillId,
    string RequiredSkillName,
    int RequiredMinNiveau);
