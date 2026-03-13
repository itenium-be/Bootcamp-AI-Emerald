using Itenium.SkillForge.Entities.Goals;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.Services.Activity;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.Data;

/// <summary>
/// EF Core implementation of <see cref="IActivityService"/>.
/// Issues #54 (consultant activity history) and #57 (seniority progress / team members).
/// </summary>
internal sealed class ActivityService : IActivityService
{
    private readonly AppDbContext _db;

    public ActivityService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ActivityEventDto>> GetActivityAsync(
        int consultantId,
        CancellationToken ct = default)
    {
        var userId = await _db.Consultants
            .Where(c => c.Id == consultantId)
            .Select(c => (string?)c.UserId)
            .FirstOrDefaultAsync(ct);

        if (userId is null) return [];

        var events = new List<ActivityEventDto>();

        // Skill validations
        var validations = await _db.SkillValidations
            .Where(v => v.ConsultantUserId == userId)
            .Include(v => v.Skill)
            .ToListAsync(ct);

        events.AddRange(validations.Select(v => new ActivityEventDto(
            ActivityEventType.SkillValidated,
            v.ValidatedAt,
            $"Validated {v.Skill.Name} to niveau {v.Niveau}",
            v.Skill.Name,
            v.Niveau,
            null)));

        // Achieved goals (only those with AchievedAt set)
        var achievedGoals = await _db.Goals
            .Where(g => g.ConsultantUserId == userId
                        && g.Status == GoalStatus.Achieved
                        && g.AchievedAt != null)
            .Include(g => g.Skill)
            .ToListAsync(ct);

        events.AddRange(achievedGoals.Select(g => new ActivityEventDto(
            ActivityEventType.GoalAchieved,
            g.AchievedAt!.Value,
            $"Achieved goal: {g.Skill.Name} to niveau {g.TargetNiveau}",
            g.Skill.Name,
            g.TargetNiveau,
            null)));

        // Resource completions
        var completions = await _db.ResourceCompletions
            .Where(c => c.UserId == userId)
            .Include(c => c.Resource)
            .ToListAsync(ct);

        events.AddRange(completions.Select(c => new ActivityEventDto(
            ActivityEventType.ResourceCompleted,
            c.CompletedAt,
            $"Completed resource: {c.Resource.Title}",
            null,
            null,
            c.Resource.Title)));

        return [.. events.OrderByDescending(e => e.OccurredAt)];
    }

    public async Task<IReadOnlyList<ConsultantSummaryDto>> GetTeamMembersAsync(
        ITeamQueryScope scope,
        CancellationToken ct = default)
    {
        var query = _db.Consultants
            .Where(c => !c.ArchivedAt.HasValue)
            .ApplyTeamScope(scope);

        var consultants = await query
            .Include(c => c.Profile)
            .Select(c => new
            {
                c.Id,
                c.UserId,
                c.TeamId,
                ProfileName = c.Profile != null ? c.Profile.Name : null,
                ActiveGoalCount = _db.Goals.Count(g => g.ConsultantUserId == c.UserId && g.Status == GoalStatus.Active),
                ActiveFlagCount = _db.ReadinessFlags.Count(f => f.Goal.ConsultantUserId == c.UserId && f.DismissedAt == null),
            })
            .ToListAsync(ct);

        if (consultants.Count == 0) return [];

        // Fetch team names and user emails separately to avoid complex joins
        var teamIds = consultants.Select(c => c.TeamId).Distinct().ToList();
        var teams = await _db.Teams
            .Where(t => teamIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Name })
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        var userIds = consultants.Select(c => c.UserId).ToList();
        var emails = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToDictionaryAsync(u => u.Id, u => u.Email, ct);

        return consultants.Select(c => new ConsultantSummaryDto(
            c.Id,
            c.UserId,
            emails.GetValueOrDefault(c.UserId),
            c.ProfileName,
            teams.GetValueOrDefault(c.TeamId, "Unknown"),
            c.ActiveGoalCount,
            c.ActiveFlagCount)).ToList();
    }
}
