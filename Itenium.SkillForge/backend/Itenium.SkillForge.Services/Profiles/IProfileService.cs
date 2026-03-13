using Itenium.SkillForge.Services.SkillCatalogue;

namespace Itenium.SkillForge.Services.Profiles;

/// <summary>
/// Provides read access to competence centre profiles, their skill subsets,
/// seniority thresholds, and the ability to assign a profile to a consultant.
/// </summary>
public interface IProfileService
{
    /// <summary>Returns all competence centre profiles, ordered by name.</summary>
    Task<IReadOnlyList<ProfileListItem>> GetProfilesAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the skills belonging to the given profile, ordered by category then name.
    /// Returns <see langword="null"/> when the profile does not exist.
    /// </summary>
    Task<IReadOnlyList<SkillListItem>?> GetProfileSkillsAsync(int profileId, CancellationToken ct = default);

    /// <summary>
    /// Returns seniority thresholds for the profile grouped by seniority level.
    /// Returns <see langword="null"/> when the profile does not exist.
    /// </summary>
    Task<IReadOnlyList<SeniorityThresholdsForLevel>?> GetSeniorityThresholdsAsync(int profileId, CancellationToken ct = default);

    /// <summary>
    /// Assigns (or clears) the profile for a consultant.
    /// Returns <see langword="false"/> when the consultant does not exist.
    /// </summary>
    Task<bool> AssignProfileToConsultantAsync(int consultantId, int? profileId, CancellationToken ct = default);
}
