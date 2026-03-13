using Itenium.SkillForge.Entities.Goals;
using Itenium.SkillForge.Entities.Profiles;
using Itenium.SkillForge.Services.Roadmap;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.Data;

/// <summary>
/// EF Core implementation of <see cref="IRoadmapService"/>.
/// Issues #13 (roadmap), #14 (prerequisite warnings), #15 (seniority progress).
/// </summary>
internal sealed class RoadmapService : IRoadmapService
{
    private readonly AppDbContext _db;

    public RoadmapService(AppDbContext db)
    {
        _db = db;
    }

    // ── Roadmap (#13 + #14) ───────────────────────────────────────────────────

    public async Task<IReadOnlyList<RoadmapSkillNode>?> GetRoadmapAsync(
        int consultantId,
        bool full = false,
        CancellationToken ct = default)
    {
        var consultant = await _db.Consultants
            .FirstOrDefaultAsync(c => c.Id == consultantId, ct);

        if (consultant is null || consultant.ProfileId is null)
            return null;

        // Project profile skills directly (avoids Include + Contains incompatibility in EF Core 10)
        var skillData = await _db.ProfileSkills
            .Where(ps => ps.ProfileId == consultant.ProfileId)
            .Select(ps => new
            {
                ps.Skill.Id,
                ps.Skill.Name,
                ps.Skill.LevelCount,
                CategoryName = ps.Skill.Category.Name,
                Prerequisites = ps.Skill.Prerequisites
                    .Select(p => new { p.RequiredSkillId, RequiredSkillName = p.RequiredSkill.Name, p.RequiredMinNiveau })
                    .ToList(),
            })
            .ToListAsync(ct);

        // Current niveau per skill (max validated)
        var rawValidations = await _db.SkillValidations
            .Where(v => v.ConsultantUserId == consultant.UserId)
            .Select(v => new { v.SkillId, v.Niveau })
            .ToListAsync(ct);

        var niveauBySkill = rawValidations
            .GroupBy(v => v.SkillId)
            .ToDictionary(g => g.Key, g => g.Max(v => v.Niveau));

        // Active goals keyed by SkillId
        var activeGoals = await _db.Goals
            .Where(g => g.ConsultantUserId == consultant.UserId && g.Status == GoalStatus.Active)
            .ToDictionaryAsync(g => g.SkillId, ct);

        // Build all nodes
        var allNodes = skillData.Select(skill =>
        {
            var currentNiveau = niveauBySkill.GetValueOrDefault(skill.Id, 0);
            var targetNiveau = activeGoals.TryGetValue(skill.Id, out var goal)
                ? (int?)goal.TargetNiveau
                : null;

            var unmet = skill.Prerequisites
                .Where(p => niveauBySkill.GetValueOrDefault(p.RequiredSkillId, 0) < p.RequiredMinNiveau)
                .Select(p => new SkillPrerequisiteWarning(
                    p.RequiredSkillId,
                    p.RequiredSkillName,
                    p.RequiredMinNiveau,
                    niveauBySkill.GetValueOrDefault(p.RequiredSkillId, 0)))
                .ToList();

            return new RoadmapSkillNode(
                skill.Id,
                skill.Name,
                skill.CategoryName,
                skill.LevelCount,
                currentNiveau,
                targetNiveau,
                unmet.Count == 0,
                unmet);
        }).ToList();

        if (full) return allNodes;

        return ProgressiveView(allNodes);
    }

    /// <summary>
    /// Selects 8–12 nodes for the default "progressive disclosure" view.
    /// Priority: anchors (validated) → unlocked (all prerequisites met) → fewest unmet prerequisites.
    /// </summary>
    private static List<RoadmapSkillNode> ProgressiveView(
        List<RoadmapSkillNode> allNodes)
    {
        const int min = 8;
        const int max = 12;

        var anchors = allNodes.Where(n => n.CurrentNiveau > 0).ToList();
        var unlocked = allNodes.Where(n => n.CurrentNiveau == 0 && n.PrerequisitesMet).ToList();
        var selected = anchors.Concat(unlocked).ToList();

        if (selected.Count < min && allNodes.Count > min)
        {
            var selectedIds = selected.Select(n => n.SkillId).ToHashSet();
            var extra = allNodes
                .Where(n => !selectedIds.Contains(n.SkillId))
                .OrderBy(n => n.UnmetPrerequisites.Count)
                .ThenBy(n => n.SkillName, StringComparer.Ordinal)
                .Take(min - selected.Count);
            selected.AddRange(extra);
        }

        return selected.Count > max ? selected.Take(max).ToList() : selected;
    }

    // ── Seniority progress (#15) ──────────────────────────────────────────────

    public async Task<SeniorityProgressResult?> GetSeniorityProgressAsync(
        int consultantId,
        CancellationToken ct = default)
    {
        var consultant = await _db.Consultants
            .FirstOrDefaultAsync(c => c.Id == consultantId, ct);

        if (consultant is null || consultant.ProfileId is null)
            return null;

        var thresholds = await _db.SeniorityThresholds
            .Include(t => t.Skill)
            .Where(t => t.ProfileId == consultant.ProfileId)
            .ToListAsync(ct);

        var rawValidations = await _db.SkillValidations
            .Where(v => v.ConsultantUserId == consultant.UserId)
            .Select(v => new { v.SkillId, v.Niveau })
            .ToListAsync(ct);

        var niveauBySkill = rawValidations
            .GroupBy(v => v.SkillId)
            .ToDictionary(g => g.Key, g => g.Max(v => v.Niveau));

        // Evaluate each seniority level in ascending order
        var levels = Enum.GetValues<SeniorityLevel>().OrderBy(l => (int)l).ToList();

        SeniorityLevel? currentLevel = null;
        foreach (var level in levels)
        {
            var criteria = thresholds.Where(t => t.SeniorityLevel == level).ToList();
            var allMet = criteria.All(t => niveauBySkill.GetValueOrDefault(t.SkillId, 0) >= t.MinNiveau);

            if (allMet)
                currentLevel = level;
            else
                break; // First unmet level is the "next" target
        }

        // Determine the next level to target
        SeniorityLevel? nextLevel = currentLevel switch
        {
            null => SeniorityLevel.Junior,
            SeniorityLevel.Junior => SeniorityLevel.Medior,
            SeniorityLevel.Medior => SeniorityLevel.Senior,
            SeniorityLevel.Senior => null,
            _ => null,
        };

        if (nextLevel is null)
        {
            // Already Senior — return with Met == Required
            var seniorCriteria = thresholds
                .Where(t => t.SeniorityLevel == SeniorityLevel.Senior)
                .ToList();
            return new SeniorityProgressResult(
                SeniorityLevel.Senior,
                null,
                seniorCriteria.Count,
                seniorCriteria.Count,
                []);
        }

        var nextCriteria = thresholds.Where(t => t.SeniorityLevel == nextLevel.Value).ToList();
        var unmet = nextCriteria
            .Where(t => niveauBySkill.GetValueOrDefault(t.SkillId, 0) < t.MinNiveau)
            .Select(t => new SeniorityProgressCriterion(
                t.SkillId,
                t.Skill.Name,
                t.MinNiveau,
                niveauBySkill.GetValueOrDefault(t.SkillId, 0)))
            .ToList();

        return new SeniorityProgressResult(
            currentLevel,
            nextLevel,
            nextCriteria.Count - unmet.Count,
            nextCriteria.Count,
            unmet);
    }
}
