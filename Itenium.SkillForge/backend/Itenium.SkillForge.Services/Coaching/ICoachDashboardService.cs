namespace Itenium.SkillForge.Services.Coaching;

public interface ICoachDashboardService
{
    Task<IReadOnlyList<CoachDashboardRow>> GetDashboardAsync(ITeamQueryScope scope, CancellationToken ct = default);

    /// <summary>Returns null if consultant not found or out of scope.</summary>
    Task<ConsultantActivityHistory?> GetActivityAsync(int consultantId, ITeamQueryScope scope, CancellationToken ct = default);
}

public record CoachDashboardRow(
    int ConsultantId,
    string UserId,
    int ActiveGoalCount,
    int OverdueGoalCount,
    int ReadinessFlagCount,
    int FlagAgeMaxDays,
    DateTime? LastActivityAt,
    bool IsInactive);

public record ConsultantActivityHistory(
    int ConsultantId,
    string UserId,
    IReadOnlyList<ActivityItem> Items);

public record ActivityItem(
    string Type,
    string Description,
    DateTime OccurredAt);
