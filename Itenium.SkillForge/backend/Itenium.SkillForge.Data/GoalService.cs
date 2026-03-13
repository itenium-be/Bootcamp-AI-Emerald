using Itenium.SkillForge.Entities.Goals;
using Itenium.SkillForge.Services.Goals;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.Data;

/// <summary>
/// EF Core implementation of <see cref="IGoalService"/>.
/// </summary>
internal sealed class GoalService : IGoalService
{
    private readonly AppDbContext _db;

    public GoalService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<GoalDto>> GetGoalsForConsultantAsync(int consultantId, CancellationToken ct = default)
    {
        var userId = await _db.Consultants
            .Where(c => c.Id == consultantId)
            .Select(c => (string?)c.UserId)
            .FirstOrDefaultAsync(ct);

        if (userId is null) return [];

        return await FetchGoalsAsync(_db.Goals.Where(g => g.ConsultantUserId == userId), userId, ct);
    }

    public async Task<GoalDto?> GetGoalAsync(int goalId, CancellationToken ct = default)
    {
        var userId = await _db.Goals
            .Where(g => g.Id == goalId)
            .Select(g => (string?)g.ConsultantUserId)
            .FirstOrDefaultAsync(ct);

        if (userId is null) return null;

        var goals = await FetchGoalsAsync(_db.Goals.Where(g => g.Id == goalId), userId, ct);
        return goals.Count > 0 ? goals[0] : null;
    }

    public async Task<GoalDto?> CreateGoalAsync(int consultantId, CreateGoalRequest request, string coachUserId, CancellationToken ct = default)
    {
        var userId = await _db.Consultants
            .Where(c => c.Id == consultantId)
            .Select(c => (string?)c.UserId)
            .FirstOrDefaultAsync(ct);

        if (userId is null) return null;

        var goal = new GoalEntity
        {
            ConsultantUserId = userId,
            CoachUserId = coachUserId,
            SkillId = request.SkillId,
            CurrentNiveau = request.CurrentNiveau,
            TargetNiveau = request.TargetNiveau,
            Deadline = request.Deadline,
        };

        _db.Goals.Add(goal);
        await _db.SaveChangesAsync(ct);

        if (request.ResourceIds is { Count: > 0 })
        {
            foreach (var resourceId in request.ResourceIds)
                _db.GoalResources.Add(new GoalResourceEntity { GoalId = goal.Id, ResourceId = resourceId });
            await _db.SaveChangesAsync(ct);
        }

        return await GetGoalAsync(goal.Id, ct);
    }

    public async Task<GoalDto?> UpdateGoalAsync(int goalId, UpdateGoalRequest request, CancellationToken ct = default)
    {
        var goal = await _db.Goals.FindAsync([goalId], ct);
        if (goal is null) return null;

        goal.CurrentNiveau = request.CurrentNiveau;
        goal.TargetNiveau = request.TargetNiveau;
        goal.Deadline = request.Deadline;
        goal.Status = request.Status;

        await _db.SaveChangesAsync(ct);
        return await GetGoalAsync(goalId, ct);
    }

    public async Task<bool> AddResourceToGoalAsync(int goalId, int resourceId, CancellationToken ct = default)
    {
        var goalExists = await _db.Goals.AnyAsync(g => g.Id == goalId, ct);
        if (!goalExists) return false;

        var alreadyLinked = await _db.GoalResources
            .AnyAsync(gr => gr.GoalId == goalId && gr.ResourceId == resourceId, ct);
        if (alreadyLinked) return true;

        _db.GoalResources.Add(new GoalResourceEntity { GoalId = goalId, ResourceId = resourceId });
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RemoveResourceFromGoalAsync(int goalId, int resourceId, CancellationToken ct = default)
    {
        var link = await _db.GoalResources
            .FirstOrDefaultAsync(gr => gr.GoalId == goalId && gr.ResourceId == resourceId, ct);
        if (link is null) return false;

        _db.GoalResources.Remove(link);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<IReadOnlyList<GoalDto>> FetchGoalsAsync(
        IQueryable<GoalEntity> baseQuery,
        string consultantUserId,
        CancellationToken ct)
    {
        var goals = await baseQuery
            .Include(g => g.Skill)
            .Include(g => g.GoalResources)
                .ThenInclude(gr => gr.Resource)
            .Include(g => g.ReadinessFlag)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(ct);

        var resourceIds = goals
            .SelectMany(g => g.GoalResources.Select(gr => gr.ResourceId))
            .Distinct()
            .ToList();

        var completedSet = new HashSet<int>();
        if (resourceIds.Count > 0)
        {
            var completed = await _db.ResourceCompletions
                .Where(rc => rc.UserId == consultantUserId && resourceIds.Contains(rc.ResourceId))
                .Select(rc => rc.ResourceId)
                .Distinct()
                .ToListAsync(ct);
            completedSet = [.. completed];
        }

        return goals.Select(g => ToDto(g, completedSet)).ToList();
    }

    private static GoalDto ToDto(GoalEntity g, HashSet<int> completedResourceIds)
    {
        var resources = g.GoalResources
            .Select(gr => new LinkedResourceDto(
                gr.ResourceId,
                gr.Resource.Title,
                gr.Resource.Url,
                gr.Resource.Type,
                completedResourceIds.Contains(gr.ResourceId)))
            .ToList();

        ReadinessFlagDto? flagDto = null;
        if (g.ReadinessFlag is { DismissedAt: null } flag)
        {
            flagDto = new ReadinessFlagDto(
                flag.Id,
                flag.RaisedAt,
                (int)(DateTime.UtcNow - flag.RaisedAt).TotalDays);
        }

        return new GoalDto(
            g.Id,
            g.ConsultantUserId,
            g.CoachUserId,
            g.SkillId,
            g.Skill.Name,
            g.CurrentNiveau,
            g.TargetNiveau,
            g.Deadline,
            g.Status,
            g.CreatedAt,
            resources,
            flagDto);
    }
}
