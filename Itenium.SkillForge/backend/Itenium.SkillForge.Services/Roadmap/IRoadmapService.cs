namespace Itenium.SkillForge.Services.Roadmap;

/// <summary>
/// Computes a consultant's skill roadmap and seniority progress.
/// </summary>
public interface IRoadmapService
{
    /// <summary>
    /// Returns the consultant's roadmap for their assigned profile.
    /// Default (full=false) returns anchored skills + unlocked next-tier skills (8–12 nodes).
    /// Full view returns all profile skills.
    /// Returns null when the consultant does not exist or has no profile assigned.
    /// </summary>
    Task<IReadOnlyList<RoadmapSkillNode>?> GetRoadmapAsync(
        int consultantId,
        bool full = false,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the consultant's progress toward their next seniority level.
    /// Returns null when the consultant does not exist or has no profile assigned.
    /// </summary>
    Task<SeniorityProgressResult?> GetSeniorityProgressAsync(
        int consultantId,
        CancellationToken ct = default);
}
