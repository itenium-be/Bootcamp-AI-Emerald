using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Entities.Goals;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GoalController(AppDbContext db, ITeamQueryScope scope, ISkillForgeUser user) : ControllerBase
{
    private readonly AppDbContext _db = db;
    private readonly ITeamQueryScope _scope = scope;
    private readonly ISkillForgeUser _user = user;

    /// <summary>Get the current learner's own active goals.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<GoalEntity>>> GetMyGoals()
    {
        var userId = _user.UserId;
        var goals = await _db.Goals
            .Include(g => g.Skill)
            .Include(g => g.GoalResources).ThenInclude(gr => gr.Resource)
            .Include(g => g.ReadinessFlag)
            .Where(g => g.ConsultantUserId == userId && g.Status == GoalStatus.Active)
            .ToListAsync();
        return Ok(goals);
    }

    /// <summary>Get goals for a specific consultant (manager view).</summary>
    [HttpGet("consultant/{consultantUserId}")]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    public async Task<ActionResult<List<GoalEntity>>> GetConsultantGoals(string consultantUserId)
    {
        var consultant = await _db.Consultants.FirstOrDefaultAsync(c => c.UserId == consultantUserId);
        if (consultant == null) return NotFound();
        if (!_scope.IsBackOffice && !_scope.TeamIds.Contains(consultant.TeamId)) return Forbid();

        var goals = await _db.Goals
            .Include(g => g.Skill)
            .Include(g => g.GoalResources).ThenInclude(gr => gr.Resource)
            .Include(g => g.ReadinessFlag)
            .Where(g => g.ConsultantUserId == consultantUserId)
            .ToListAsync();
        return Ok(goals);
    }

    /// <summary>Create a goal for a consultant (manager).</summary>
    [HttpPost]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    public async Task<ActionResult<GoalEntity>> CreateGoal([FromBody] CreateGoalRequest request)
    {
        var consultant = await _db.Consultants.FirstOrDefaultAsync(c => c.UserId == request.ConsultantUserId);
        if (consultant == null) return NotFound();
        if (!_scope.IsBackOffice && !_scope.TeamIds.Contains(consultant.TeamId)) return Forbid();

        var goal = new GoalEntity
        {
            ConsultantUserId = request.ConsultantUserId,
            CoachUserId = _user.UserId ?? string.Empty,
            SkillId = request.SkillId,
            CurrentNiveau = request.CurrentNiveau,
            TargetNiveau = request.TargetNiveau,
            Deadline = request.Deadline,
        };
        _db.Goals.Add(goal);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetConsultantGoals), new { consultantUserId = request.ConsultantUserId }, goal);
    }

    /// <summary>Update a goal (manager).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    public async Task<ActionResult<GoalEntity>> UpdateGoal(int id, [FromBody] UpdateGoalRequest request)
    {
        var goal = await _db.Goals.FindAsync(id);
        if (goal == null) return NotFound();

        goal.TargetNiveau = request.TargetNiveau;
        goal.Deadline = request.Deadline;
        if (request.Status.HasValue)
            goal.Status = request.Status.Value;

        await _db.SaveChangesAsync();
        return Ok(goal);
    }

    /// <summary>Delete a goal (manager/backoffice).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    public async Task<IActionResult> DeleteGoal(int id)
    {
        var goal = await _db.Goals.FindAsync(id);
        if (goal == null) return NotFound();

        _db.Goals.Remove(goal);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Learner signals readiness (only if goal is theirs and no active flag).</summary>
    [HttpPost("{id:int}/readiness")]
    public async Task<IActionResult> SignalReadiness(int id)
    {
        var userId = _user.UserId;
        var goal = await _db.Goals
            .Include(g => g.ReadinessFlag)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (goal == null) return NotFound();
        if (goal.ConsultantUserId != userId) return Forbid();
        if (goal.ReadinessFlag != null && goal.ReadinessFlag.DismissedAt == null)
            return BadRequest("A readiness flag already exists for this goal.");

        var flag = new ReadinessFlagEntity { GoalId = id };
        _db.ReadinessFlags.Add(flag);
        await _db.SaveChangesAsync();
        return Created(string.Empty, flag);
    }

    /// <summary>Manager dismisses readiness flag.</summary>
    [HttpDelete("{id:int}/readiness")]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    public async Task<IActionResult> DismissReadiness(int id)
    {
        var goal = await _db.Goals
            .Include(g => g.ReadinessFlag)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (goal == null) return NotFound();
        if (goal.ReadinessFlag == null) return NotFound();

        goal.ReadinessFlag.DismissedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Link a resource to a goal (manager).</summary>
    [HttpPost("{id:int}/resources/{resourceId:int}")]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    public async Task<IActionResult> AddResourceToGoal(int id, int resourceId)
    {
        var goal = await _db.Goals.FindAsync(id);
        if (goal == null) return NotFound();

        var resource = await _db.Resources.FindAsync(resourceId);
        if (resource == null) return NotFound();

        var alreadyLinked = await _db.GoalResources
            .AnyAsync(gr => gr.GoalId == id && gr.ResourceId == resourceId);
        if (!alreadyLinked)
        {
            _db.GoalResources.Add(new GoalResourceEntity { GoalId = id, ResourceId = resourceId });
            await _db.SaveChangesAsync();
        }
        return NoContent();
    }

    /// <summary>Unlink a resource from a goal (manager).</summary>
    [HttpDelete("{id:int}/resources/{resourceId:int}")]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    public async Task<IActionResult> RemoveResourceFromGoal(int id, int resourceId)
    {
        var link = await _db.GoalResources
            .FirstOrDefaultAsync(gr => gr.GoalId == id && gr.ResourceId == resourceId);
        if (link == null) return NotFound();

        _db.GoalResources.Remove(link);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Get overdue active goals scoped to manager's teams.</summary>
    [HttpGet("overdue")]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    public async Task<ActionResult<List<GoalEntity>>> GetOverdueGoals()
    {
        var consultantUserIds = await _db.Consultants
            .Where(c => _scope.IsBackOffice || _scope.TeamIds.Contains(c.TeamId))
            .Select(c => c.UserId)
            .ToListAsync();

        var goals = await _db.Goals
            .Include(g => g.Skill)
            .Where(g => consultantUserIds.Contains(g.ConsultantUserId)
                        && g.Status == GoalStatus.Active
                        && g.Deadline.HasValue
                        && g.Deadline < DateTime.UtcNow)
            .ToListAsync();
        return Ok(goals);
    }
}

public record CreateGoalRequest(string ConsultantUserId, int SkillId, int CurrentNiveau, int TargetNiveau, DateTime? Deadline);
public record UpdateGoalRequest(int TargetNiveau, DateTime? Deadline, GoalStatus? Status);
