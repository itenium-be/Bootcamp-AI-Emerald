namespace Itenium.SkillForge.Services.Import;

/// <summary>Counts of entities created during an import run.</summary>
public sealed record SkillImportResult(
    int CategoriesCreated,
    int SkillsCreated,
    int LevelsCreated,
    int PrerequisitesCreated,
    int ProfilesCreated,
    int ProfileSkillsCreated,
    int ThresholdsCreated);
