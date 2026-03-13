using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.Services.Coaching;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.Data;

public class CoachingSessionService : ICoachingSessionService
{
    private readonly AppDbContext _db;

    public CoachingSessionService(AppDbContext db) => _db = db;

    public async Task<CoachingSessionRecord?> StartSessionAsync(
        int consultantId, string coachUserId, ITeamQueryScope scope, CancellationToken ct = default)
    {
        var consultant = await _db.Consultants
            .ApplyTeamScope(scope)
            .FirstOrDefaultAsync(c => c.Id == consultantId && c.ArchivedAt == null, ct);

        if (consultant is null) return null;

        var session = new CoachingSessionEntity
        {
            ConsultantUserId = consultant.UserId,
            CoachUserId = coachUserId,
        };

        _db.CoachingSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        return ToRecord(session);
    }

    public async Task<bool> UpdateNotesAsync(int sessionId, string notes, CancellationToken ct = default)
    {
        var session = await _db.CoachingSessions.FindAsync([sessionId], ct);
        if (session is null) return false;

        session.Notes = notes;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> CloseSessionAsync(int sessionId, CancellationToken ct = default)
    {
        var session = await _db.CoachingSessions.FindAsync([sessionId], ct);
        if (session is null) return false;

        session.ClosedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<CoachingSessionRecord>?> GetSessionsAsync(
        int consultantId, ITeamQueryScope scope, CancellationToken ct = default)
    {
        var consultant = await _db.Consultants
            .ApplyTeamScope(scope)
            .FirstOrDefaultAsync(c => c.Id == consultantId && c.ArchivedAt == null, ct);

        if (consultant is null) return null;

        return await _db.CoachingSessions
            .Where(s => s.ConsultantUserId == consultant.UserId)
            .OrderByDescending(s => s.StartedAt)
            .Select(s => ToRecord(s))
            .ToListAsync(ct);
    }

    private static CoachingSessionRecord ToRecord(CoachingSessionEntity s) =>
        new(s.Id, s.ConsultantUserId, s.CoachUserId, s.StartedAt, s.ClosedAt, s.Notes);
}
