namespace Itenium.SkillForge.Services.Goals;

/// <summary>
/// Manages coaching goals assigned to consultants.
/// </summary>
public interface IGoalService
{
    /// <summary>
    /// Returns all goals for the given consultant, ordered newest first.
    /// Returns an empty list when the consultant does not exist.
    /// </summary>
    Task<IReadOnlyList<GoalDto>> GetGoalsForConsultantAsync(int consultantId, CancellationToken ct = default);

    /// <summary>
    /// Returns a single goal with linked resources and active readiness flag,
    /// or <c>null</c> when not found.
    /// </summary>
    Task<GoalDto?> GetGoalAsync(int goalId, CancellationToken ct = default);

    /// <summary>
    /// Creates a goal for the given consultant. Returns <c>null</c> when the consultant does not exist.
    /// </summary>
    Task<GoalDto?> CreateGoalAsync(int consultantId, CreateGoalRequest request, string coachUserId, CancellationToken ct = default);

    /// <summary>
    /// Replaces the editable fields of a goal. Returns <c>null</c> when the goal does not exist.
    /// </summary>
    Task<GoalDto?> UpdateGoalAsync(int goalId, UpdateGoalRequest request, CancellationToken ct = default);

    /// <summary>
    /// Links an existing resource to a goal. Returns <c>false</c> when the goal does not exist.
    /// Idempotent: returns <c>true</c> if already linked.
    /// </summary>
    Task<bool> AddResourceToGoalAsync(int goalId, int resourceId, CancellationToken ct = default);

    /// <summary>
    /// Removes a resource link from a goal. Returns <c>false</c> when the link does not exist.
    /// </summary>
    Task<bool> RemoveResourceFromGoalAsync(int goalId, int resourceId, CancellationToken ct = default);
}
