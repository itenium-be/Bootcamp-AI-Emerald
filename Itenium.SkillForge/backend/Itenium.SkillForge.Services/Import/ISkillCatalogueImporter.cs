namespace Itenium.SkillForge.Services.Import;

/// <summary>
/// Imports the skill catalogue from CSV content strings.
/// Idempotent: running multiple times does not create duplicate records.
/// </summary>
public interface ISkillCatalogueImporter
{
    /// <summary>
    /// Parses the CSV content and upserts all skills, levels, prerequisites,
    /// profiles, profile-skill mappings, and seniority thresholds.
    /// Returns counts of newly created entities (0 for each when re-run).
    /// </summary>
    Task<SkillImportResult> ImportAsync(ParsedCatalogue catalogue, CancellationToken ct = default);
}
