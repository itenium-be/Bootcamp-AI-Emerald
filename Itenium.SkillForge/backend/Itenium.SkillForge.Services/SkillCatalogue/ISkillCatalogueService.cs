namespace Itenium.SkillForge.Services.SkillCatalogue;

/// <summary>
/// Read-only query service for the global skill catalogue.
/// Optionally filtered by category or competence centre profile.
/// </summary>
public interface ISkillCatalogueService
{
    /// <summary>
    /// Returns all skills, optionally filtered by <paramref name="categoryId"/>
    /// or <paramref name="profileId"/>.
    /// </summary>
    Task<IReadOnlyList<SkillListItem>> GetSkillsAsync(
        int? categoryId = null,
        int? profileId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the full detail of a skill including level descriptors and prerequisites,
    /// or <c>null</c> if the skill does not exist.
    /// </summary>
    Task<SkillDetail?> GetSkillDetailAsync(int id, CancellationToken ct = default);
}
