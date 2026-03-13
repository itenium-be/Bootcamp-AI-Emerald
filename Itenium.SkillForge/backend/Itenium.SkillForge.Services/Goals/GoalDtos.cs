using Itenium.SkillForge.Entities.Goals;
using Itenium.SkillForge.Entities.Resources;

namespace Itenium.SkillForge.Services.Goals;

public record GoalDto(
    int Id,
    string ConsultantUserId,
    string CoachUserId,
    int SkillId,
    string SkillName,
    int CurrentNiveau,
    int TargetNiveau,
    DateTime? Deadline,
    GoalStatus Status,
    DateTime CreatedAt,
    IReadOnlyList<LinkedResourceDto> Resources,
    ReadinessFlagDto? ActiveReadinessFlag);

public record LinkedResourceDto(
    int ResourceId,
    string Title,
    string Url,
    ResourceType Type,
    bool IsCompleted);

public record ReadinessFlagDto(
    int Id,
    DateTime RaisedAt,
    int AgeDays);

public record CreateGoalRequest(
    int SkillId,
    int CurrentNiveau,
    int TargetNiveau,
    DateTime? Deadline,
    IReadOnlyList<int>? ResourceIds);

public record UpdateGoalRequest(
    int CurrentNiveau,
    int TargetNiveau,
    DateTime? Deadline,
    GoalStatus Status);
