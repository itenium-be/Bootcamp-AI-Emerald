namespace Itenium.SkillForge.Services.Coaching;

public interface ISkillValidationService
{
    /// <summary>
    /// Records an immutable skill validation for a consultant.
    /// Returns null if the consultant is not found or not in scope.
    /// </summary>
    Task<SkillValidationRecord?> ValidateSkillAsync(
        int consultantId,
        string coachUserId,
        int skillId,
        int newNiveau,
        int? sessionId,
        ITeamQueryScope scope,
        CancellationToken ct = default);
}

public record SkillValidationRecord(
    int Id,
    string ConsultantUserId,
    int SkillId,
    int Niveau,
    DateTime ValidatedAt,
    int? SessionId);
