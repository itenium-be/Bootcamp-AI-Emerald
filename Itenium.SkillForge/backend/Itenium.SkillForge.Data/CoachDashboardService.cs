using Itenium.SkillForge.Entities.Goals;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.Services.Coaching;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.Data;

public class CoachDashboardService : ICoachDashboardService
{
    private readonly AppDbContext _db;
    private static readonly TimeSpan InactivityThreshold = TimeSpan.FromDays(21);

    public CoachDashboardService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CoachDashboardRow>> GetDashboardAsync(
        ITeamQueryScope scope, CancellationToken ct = default)
    {
        var consultants = await _db.Consultants
            .ApplyTeamScope(scope)
            .Where(c => c.ArchivedAt == null)
            .Select(c => new { c.Id, c.UserId })
            .ToListAsync(ct);

        var userIds = consultants.Select(c => c.UserId).ToList();
        var today = DateTime.UtcNow;

        var activeGoalCounts = await _db.Goals
            .Where(g => userIds.Contains(g.ConsultantUserId) && g.Status == GoalStatus.Active)
            .GroupBy(g => g.ConsultantUserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var overdueGoalCounts = await _db.Goals
            .Where(g => userIds.Contains(g.ConsultantUserId)
                     && g.Status == GoalStatus.Active
                     && g.Deadline != null && g.Deadline < today)
            .GroupBy(g => g.ConsultantUserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var activeFlagData = await _db.ReadinessFlags
            .Where(f => f.DismissedAt == null)
            .Join(_db.Goals.Where(g => userIds.Contains(g.ConsultantUserId)),
                f => f.GoalId, g => g.Id,
                (f, g) => new { g.ConsultantUserId, f.RaisedAt })
            .ToListAsync(ct);

        var resourceLastActivity = await _db.ResourceCompletions
            .Where(rc => userIds.Contains(rc.UserId))
            .GroupBy(rc => rc.UserId)
            .Select(g => new { UserId = g.Key, LastAt = g.Max(x => x.CompletedAt) })
            .ToListAsync(ct);

        var flagLastActivity = await _db.ReadinessFlags
            .Join(_db.Goals.Where(g => userIds.Contains(g.ConsultantUserId)),
                f => f.GoalId, g => g.Id,
                (f, g) => new { g.ConsultantUserId, f.RaisedAt })
            .GroupBy(x => x.ConsultantUserId)
            .Select(g => new { UserId = g.Key, LastAt = g.Max(x => x.RaisedAt) })
            .ToListAsync(ct);

        var goalLastActivity = await _db.Goals
            .Where(g => userIds.Contains(g.ConsultantUserId))
            .GroupBy(g => g.ConsultantUserId)
            .Select(g => new { UserId = g.Key, LastAt = g.Max(x => x.CreatedAt) })
            .ToListAsync(ct);

        var rows = consultants.Select(c =>
        {
            var activeGoals = activeGoalCounts.FirstOrDefault(x => x.UserId == c.UserId)?.Count ?? 0;
            var overdueGoals = overdueGoalCounts.FirstOrDefault(x => x.UserId == c.UserId)?.Count ?? 0;

            var flags = activeFlagData.Where(f => f.ConsultantUserId == c.UserId).ToList();
            var flagCount = flags.Count;
            var flagAgeMax = flagCount > 0
                ? (int)(today - flags.Min(f => f.RaisedAt)).TotalDays
                : 0;

            var lastDates = new List<DateTime?> {
                resourceLastActivity.FirstOrDefault(x => x.UserId == c.UserId)?.LastAt,
                flagLastActivity.FirstOrDefault(x => x.UserId == c.UserId)?.LastAt,
                goalLastActivity.FirstOrDefault(x => x.UserId == c.UserId)?.LastAt,
            };
            var lastActivityAt = lastDates.Where(d => d.HasValue).Select(d => d!.Value)
                .Cast<DateTime?>()
                .MaxBy(d => d);

            var isInactive = lastActivityAt == null
                || (today - lastActivityAt.Value) > InactivityThreshold;

            return new CoachDashboardRow(
                c.Id, c.UserId,
                activeGoals, overdueGoals,
                flagCount, flagAgeMax,
                lastActivityAt, isInactive);
        }).ToList();

        return rows;
    }

    public async Task<ConsultantActivityHistory?> GetActivityAsync(
        int consultantId, ITeamQueryScope scope, CancellationToken ct = default)
    {
        var consultant = await _db.Consultants
            .ApplyTeamScope(scope)
            .FirstOrDefaultAsync(c => c.Id == consultantId && c.ArchivedAt == null, ct);

        if (consultant is null) return null;

        var items = new List<ActivityItem>();

        var completions = await _db.ResourceCompletions
            .Include(rc => rc.Resource)
            .Where(rc => rc.UserId == consultant.UserId)
            .ToListAsync(ct);

        items.AddRange(completions.Select(rc =>
            new ActivityItem("resource_completion", $"Completed '{rc.Resource.Title}'", rc.CompletedAt)));

        var validations = await _db.SkillValidations
            .Include(v => v.Skill)
            .Where(v => v.ConsultantUserId == consultant.UserId)
            .ToListAsync(ct);

        items.AddRange(validations.Select(v =>
            new ActivityItem("validation", $"Skill '{v.Skill.Name}' validated at niveau {v.Niveau}", v.ValidatedAt)));

        var goals = await _db.Goals
            .Include(g => g.Skill)
            .Where(g => g.ConsultantUserId == consultant.UserId)
            .ToListAsync(ct);

        items.AddRange(goals.Select(g =>
            new ActivityItem("goal_created", $"Goal set: '{g.Skill.Name}' n{g.CurrentNiveau}→n{g.TargetNiveau}", g.CreatedAt)));

        var flags = await _db.ReadinessFlags
            .Include(f => f.Goal).ThenInclude(g => g.Skill)
            .Where(f => f.Goal.ConsultantUserId == consultant.UserId)
            .ToListAsync(ct);

        items.AddRange(flags.Select(f =>
            new ActivityItem("flag_raised", $"Readiness flag raised for '{f.Goal.Skill.Name}'", f.RaisedAt)));

        var sessions = await _db.CoachingSessions
            .Where(s => s.ConsultantUserId == consultant.UserId && s.ClosedAt != null)
            .ToListAsync(ct);

        items.AddRange(sessions.Select(s =>
            new ActivityItem("session", $"Coaching session recorded", s.ClosedAt!.Value)));

        return new ConsultantActivityHistory(
            consultant.Id,
            consultant.UserId,
            items.OrderByDescending(i => i.OccurredAt).ToList());
    }
}
