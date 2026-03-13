namespace Itenium.SkillForge.Services.Activity;

/// <summary>
/// Provides the consultant activity timeline and the team-scoped consultant list.
/// </summary>
public interface IActivityService
{
    /// <summary>
    /// Returns a consultant's activity timeline — skill validations, achieved goals,
    /// and completed resources — ordered newest first.
    /// Returns an empty list when the consultant does not exist.
    /// </summary>
    Task<IReadOnlyList<ActivityEventDto>> GetActivityAsync(int consultantId, CancellationToken ct = default);

    /// <summary>
    /// Returns all consultants visible to the caller (team-scoped for managers,
    /// all for backoffice), with summary counters for active goals and flags.
    /// </summary>
    Task<IReadOnlyList<ConsultantSummaryDto>> GetTeamMembersAsync(ITeamQueryScope scope, CancellationToken ct = default);
}
