using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Entities.Goals;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/coach")]
[Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
public class CoachDashboardController(AppDbContext db, ITeamQueryScope scope) : ControllerBase
{
    private readonly AppDbContext _db = db;
    private readonly ITeamQueryScope _scope = scope;

    /// <summary>Get per-consultant summary for the manager's teams.</summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<List<ConsultantDashboardRow>>> GetDashboard()
    {
        var consultants = await _db.Consultants
            .Where(c => _scope.IsBackOffice || _scope.TeamIds.Contains(c.TeamId))
            .ToListAsync();

        var rows = new List<ConsultantDashboardRow>();
        foreach (var c in consultants)
        {
            var activeGoalCount = await _db.Goals
                .CountAsync(g => g.ConsultantUserId == c.UserId && g.Status == GoalStatus.Active);

            var readinessFlagCount = await _db.ReadinessFlags
                .CountAsync(f => f.Goal.ConsultantUserId == c.UserId && f.DismissedAt == null);

            var oldestFlagRaisedAt = await _db.ReadinessFlags
                .Where(f => f.Goal.ConsultantUserId == c.UserId && f.DismissedAt == null)
                .MinAsync(f => (DateTime?)f.RaisedAt);
            int? maxFlagAgeInDays = oldestFlagRaisedAt.HasValue
                ? (int)(DateTime.UtcNow - oldestFlagRaisedAt.Value).TotalDays
                : null;

            var overdueGoalCount = await _db.Goals
                .CountAsync(g => g.ConsultantUserId == c.UserId
                                 && g.Status == GoalStatus.Active
                                 && g.Deadline.HasValue
                                 && g.Deadline < DateTime.UtcNow);

            DateTime? lastGoalUpdate = await _db.Goals
                .Where(g => g.ConsultantUserId == c.UserId)
                .MaxAsync(g => (DateTime?)g.CreatedAt);

            DateTime? lastSession = await _db.CoachingSessions
                .Where(s => s.ConsultantUserId == c.UserId && s.ClosedAt.HasValue)
                .MaxAsync(s => s.ClosedAt);

            DateTime? lastFlag = await _db.ReadinessFlags
                .Where(f => f.Goal.ConsultantUserId == c.UserId)
                .MaxAsync(f => (DateTime?)f.RaisedAt);

            DateTime? lastActivity = new[] { lastGoalUpdate, lastSession, lastFlag }
                .Where(d => d.HasValue)
                .DefaultIfEmpty()
                .Max();

            var isInactive = lastActivity.HasValue
                ? lastActivity.Value < DateTime.UtcNow.AddDays(-21)
                : true;

            rows.Add(new ConsultantDashboardRow(
                c.UserId,
                c.UserId,  // FullName: identity user lookup would require UserManager, using UserId as fallback
                activeGoalCount,
                readinessFlagCount,
                maxFlagAgeInDays,
                overdueGoalCount,
                lastActivity,
                isInactive));
        }

        return Ok(rows);
    }

    /// <summary>Get activity history for a specific consultant.</summary>
    [HttpGet("consultants/{userId}/activity")]
    public async Task<ActionResult<ConsultantActivityResponse>> GetConsultantActivity(string userId)
    {
        var consultant = await _db.Consultants.FirstOrDefaultAsync(c => c.UserId == userId);
        if (consultant == null) return NotFound();
        if (!_scope.IsBackOffice && !_scope.TeamIds.Contains(consultant.TeamId)) return Forbid();

        var goals = await _db.Goals
            .Include(g => g.Skill)
            .Where(g => g.ConsultantUserId == userId)
            .ToListAsync();

        var sessions = await _db.CoachingSessions
            .Where(s => s.ConsultantUserId == userId)
            .ToListAsync();

        var flags = await _db.ReadinessFlags
            .Where(f => f.Goal.ConsultantUserId == userId)
            .ToListAsync();

        return Ok(new ConsultantActivityResponse(goals, sessions, flags));
    }
}

public record ConsultantDashboardRow(
    string UserId,
    string FullName,
    int ActiveGoalCount,
    int ReadinessFlagCount,
    int? MaxFlagAgeInDays,
    int OverdueGoalCount,
    DateTime? LastActivityAt,
    bool IsInactive);

public record ConsultantActivityResponse(
    IReadOnlyList<GoalEntity> Goals,
    IReadOnlyList<CoachingSessionEntity> Sessions,
    IReadOnlyList<ReadinessFlagEntity> Flags);
