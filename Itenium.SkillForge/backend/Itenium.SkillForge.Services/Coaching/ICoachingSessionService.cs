namespace Itenium.SkillForge.Services.Coaching;

public interface ICoachingSessionService
{
    /// <summary>Starts a new coaching session. Returns null if consultant not found or out of scope.</summary>
    Task<CoachingSessionRecord?> StartSessionAsync(int consultantId, string coachUserId, ITeamQueryScope scope, CancellationToken ct = default);

    /// <summary>Updates session notes. Returns false if session not found.</summary>
    Task<bool> UpdateNotesAsync(int sessionId, string notes, CancellationToken ct = default);

    /// <summary>Closes a session by recording ClosedAt. Returns false if session not found.</summary>
    Task<bool> CloseSessionAsync(int sessionId, CancellationToken ct = default);

    /// <summary>Returns all sessions for a consultant. Returns null if consultant not found or out of scope.</summary>
    Task<IReadOnlyList<CoachingSessionRecord>?> GetSessionsAsync(int consultantId, ITeamQueryScope scope, CancellationToken ct = default);
}

public record CoachingSessionRecord(
    int Id,
    string ConsultantUserId,
    string CoachUserId,
    DateTime StartedAt,
    DateTime? ClosedAt,
    string? Notes);
