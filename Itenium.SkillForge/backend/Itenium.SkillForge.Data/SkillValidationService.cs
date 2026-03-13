using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.Services.Coaching;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.Data;

public class SkillValidationService : ISkillValidationService
{
    private readonly AppDbContext _db;

    public SkillValidationService(AppDbContext db) => _db = db;

    public async Task<SkillValidationRecord?> ValidateSkillAsync(
        int consultantId,
        string coachUserId,
        int skillId,
        int newNiveau,
        int? sessionId,
        ITeamQueryScope scope,
        CancellationToken ct = default)
    {
        var consultant = await _db.Consultants
            .ApplyTeamScope(scope)
            .FirstOrDefaultAsync(c => c.Id == consultantId && c.ArchivedAt == null, ct);

        if (consultant is null) return null;

        var validation = new SkillValidationEntity
        {
            ConsultantUserId = consultant.UserId,
            CoachUserId = coachUserId,
            SkillId = skillId,
            Niveau = newNiveau,
            SessionId = sessionId,
        };

        _db.SkillValidations.Add(validation);
        await _db.SaveChangesAsync(ct);

        return new SkillValidationRecord(
            validation.Id,
            validation.ConsultantUserId,
            validation.SkillId,
            validation.Niveau,
            validation.ValidatedAt,
            validation.SessionId);
    }
}
