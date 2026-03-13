namespace Itenium.SkillForge.Services.Coaching;

/// <summary>
/// Manages readiness flags — a consultant's signal that they believe they have achieved a goal level.
/// </summary>
public interface IReadinessFlagService
{
    /// <summary>
    /// Raises (or re-raises) a readiness flag for the given goal.
    /// The consultant must own the goal.
    /// </summary>
    Task<RaiseFlagResult> RaiseFlagAsync(int goalId, string consultantUserId, CancellationToken ct = default);

    /// <summary>
    /// Dismisses the active readiness flag for the given goal.
    /// Returns <c>false</c> when there is no active flag to dismiss.
    /// </summary>
    Task<bool> DismissFlagAsync(int goalId, CancellationToken ct = default);

    /// <summary>
    /// Returns all active (not dismissed) readiness flags for a consultant's goals.
    /// </summary>
    Task<IReadOnlyList<ConsultantReadinessFlagDto>> GetActiveFlagsForConsultantAsync(int consultantId, CancellationToken ct = default);
}

public enum RaiseFlagResult
{
    Success,
    GoalNotFound,
    NotOwner,
    AlreadyActive,
}
