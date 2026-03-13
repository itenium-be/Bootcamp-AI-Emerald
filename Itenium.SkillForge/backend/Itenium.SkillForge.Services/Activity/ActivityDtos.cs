namespace Itenium.SkillForge.Services.Activity;

public enum ActivityEventType
{
    SkillValidated,
    GoalAchieved,
    ResourceCompleted,
}

/// <summary>A single item in a consultant's activity timeline.</summary>
public record ActivityEventDto(
    ActivityEventType EventType,
    DateTime OccurredAt,
    string Description,
    string? SkillName,
    int? Niveau,
    string? ResourceTitle);

/// <summary>Summary card for a consultant shown on the team members page.</summary>
public record ConsultantSummaryDto(
    int Id,
    string UserId,
    string? Email,
    string? ProfileName,
    string TeamName,
    int ActiveGoalCount,
    int ActiveFlagCount);
