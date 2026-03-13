using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Services.Coaching;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.Data;

/// <summary>
/// EF Core implementation of <see cref="IReadinessFlagService"/>.
/// </summary>
internal sealed class ReadinessFlagService : IReadinessFlagService
{
    private readonly AppDbContext _db;

    public ReadinessFlagService(AppDbContext db) => _db = db;

    public async Task<RaiseFlagResult> RaiseFlagAsync(int goalId, string consultantUserId, CancellationToken ct = default)
    {
        var goal = await _db.Goals
            .Include(g => g.ReadinessFlag)
            .FirstOrDefaultAsync(g => g.Id == goalId, ct);

        if (goal is null) return RaiseFlagResult.GoalNotFound;
        if (goal.ConsultantUserId != consultantUserId) return RaiseFlagResult.NotOwner;

        if (goal.ReadinessFlag is not null)
        {
            if (goal.ReadinessFlag.DismissedAt is null) return RaiseFlagResult.AlreadyActive;

            // Re-activate dismissed flag
            goal.ReadinessFlag.DismissedAt = null;
            goal.ReadinessFlag.RaisedAt = DateTime.UtcNow;
        }
        else
        {
            _db.ReadinessFlags.Add(new ReadinessFlagEntity { GoalId = goalId });
        }

        await _db.SaveChangesAsync(ct);
        return RaiseFlagResult.Success;
    }

    public async Task<bool> DismissFlagAsync(int goalId, CancellationToken ct = default)
    {
        var flag = await _db.ReadinessFlags
            .FirstOrDefaultAsync(f => f.GoalId == goalId && f.DismissedAt == null, ct);

        if (flag is null) return false;

        flag.DismissedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<ConsultantReadinessFlagDto>> GetActiveFlagsForConsultantAsync(
        int consultantId,
        CancellationToken ct = default)
    {
        var userId = await _db.Consultants
            .Where(c => c.Id == consultantId)
            .Select(c => (string?)c.UserId)
            .FirstOrDefaultAsync(ct);

        if (userId is null) return [];

        return await _db.ReadinessFlags
            .Where(f => f.DismissedAt == null && f.Goal.ConsultantUserId == userId)
            .Select(f => new ConsultantReadinessFlagDto(
                f.Id,
                f.GoalId,
                f.Goal.SkillId,
                f.Goal.Skill.Name,
                f.Goal.TargetNiveau,
                f.RaisedAt,
                (int)(DateTime.UtcNow - f.RaisedAt).TotalDays))
            .ToListAsync(ct);
    }
}
