using System.Security.Claims;
using Itenium.SkillForge.Data;
using Itenium.SkillForge.Services.Coaching;
using Itenium.SkillForge.Services.Goals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/goals")]
[Authorize]
public class GoalsController : ControllerBase
{
    private readonly IGoalService _goals;
    private readonly IReadinessFlagService _flags;
    private readonly AppDbContext _db;

    public GoalsController(IGoalService goals, IReadinessFlagService flags, AppDbContext db)
    {
        _goals = goals;
        _flags = flags;
        _db = db;
    }

    // ── Single goal ───────────────────────────────────────────────────────────

    /// <summary>Returns a single goal with linked resources and readiness flag.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGoal(int id, CancellationToken ct = default)
    {
        var goal = await _goals.GetGoalAsync(id, ct);
        if (goal is null) return NotFound();
        return Ok(goal);
    }

    /// <summary>Updates the editable fields of a goal (coach/backoffice only).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    [ProducesResponseType(typeof(GoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGoal(
        int id,
        [FromBody] UpdateGoalRequest request,
        CancellationToken ct = default)
    {
        var result = await _goals.UpdateGoalAsync(id, request, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    // ── Resource linking (#30) ────────────────────────────────────────────────

    /// <summary>Links an existing resource to a goal.</summary>
    [HttpPost("{id:int}/resources")]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddResource(
        int id,
        [FromBody] LinkResourceRequest request,
        CancellationToken ct = default)
    {
        var success = await _goals.AddResourceToGoalAsync(id, request.ResourceId, ct);
        if (!success) return NotFound();
        return NoContent();
    }

    /// <summary>Removes a resource link from a goal.</summary>
    [HttpDelete("{id:int}/resources/{resourceId:int}")]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveResource(int id, int resourceId, CancellationToken ct = default)
    {
        var success = await _goals.RemoveResourceFromGoalAsync(id, resourceId, ct);
        if (!success) return NotFound();
        return NoContent();
    }

    // ── Readiness flags (#20) ─────────────────────────────────────────────────

    /// <summary>
    /// Raises a readiness flag — the consultant signals they believe they have achieved the goal level.
    /// </summary>
    [HttpPost("{id:int}/readiness-flag")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RaiseFlag(int id, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _flags.RaiseFlagAsync(id, userId, ct);

        return result switch
        {
            RaiseFlagResult.Success => NoContent(),
            RaiseFlagResult.GoalNotFound => NotFound(),
            RaiseFlagResult.NotOwner => Forbid(),
            RaiseFlagResult.AlreadyActive => Conflict(new { message = "A readiness flag is already active for this goal." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>
    /// Dismisses the active readiness flag (coach validates or declines the consultant's readiness signal).
    /// </summary>
    [HttpDelete("{id:int}/readiness-flag")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DismissFlag(int id, CancellationToken ct = default)
    {
        var success = await _flags.DismissFlagAsync(id, ct);
        if (!success) return NotFound();
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string GetCurrentUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? string.Empty;
}

public record LinkResourceRequest(int ResourceId);
