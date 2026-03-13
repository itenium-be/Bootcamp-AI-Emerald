namespace Itenium.SkillForge.Services.Coaching;

public record ConsultantReadinessFlagDto(
    int FlagId,
    int GoalId,
    int SkillId,
    string SkillName,
    int TargetNiveau,
    DateTime RaisedAt,
    int AgeDays);
