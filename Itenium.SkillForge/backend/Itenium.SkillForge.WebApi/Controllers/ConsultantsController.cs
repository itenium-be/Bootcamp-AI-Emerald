using System.Security.Claims;
using Itenium.SkillForge.Data;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.Services.Coaching;
using Itenium.SkillForge.Services.Goals;
using Itenium.SkillForge.Services.Profiles;
using Itenium.SkillForge.Services.Roadmap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConsultantsController : ControllerBase
{
    private readonly IProfileService _profiles;
    private readonly ITeamQueryScope _scope;
    private readonly IRoadmapService _roadmap;
    private readonly IGoalService _goals;
    private readonly IReadinessFlagService _flags;
    private readonly AppDbContext _db;

    public ConsultantsController(
        IProfileService profiles,
        ITeamQueryScope scope,
        IRoadmapService roadmap,
        IGoalService goals,
        IReadinessFlagService flags,
        AppDbContext db)
    {
        _profiles = profiles;
        _scope = scope;
        _roadmap = roadmap;
        _goals = goals;
        _flags = flags;
        _db = db;
    }

    // ── Profile assignment ────────────────────────────────────────────────────

    /// <summary>
    /// Assigns (or clears) the competence centre profile for a consultant.
    /// Pass <c>null</c> as <see cref="AssignProfileRequest.ProfileId"/> to remove the current profile.
    /// Only consultants within the caller's team scope are accessible.
    /// </summary>
    [HttpPut("{id:int}/profile")]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignProfile(
        int id,
        [FromBody] AssignProfileRequest request,
        CancellationToken ct = default)
    {
        var success = await _profiles.AssignProfileToConsultantAsync(id, request.ProfileId, _scope, ct);
        if (!success) return NotFound();
        return NoContent();
    }

    // ── Roadmap (#13, #14) ────────────────────────────────────────────────────

    /// <summary>
    /// Returns the current user's skill roadmap.
    /// Default view: anchored + unlocked skills (8–12 nodes).
    /// Full view: all profile skills.
    /// </summary>
    [HttpGet("me/roadmap")]
    [ProducesResponseType(typeof(IReadOnlyList<RoadmapSkillNode>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetMyRoadmap([FromQuery] bool full = false, CancellationToken ct = default)
        => GetRoadmapForUserAsync(GetCurrentUserId(), full, ct);

    /// <summary>
    /// Returns a consultant's skill roadmap (manager/backoffice view).
    /// </summary>
    [HttpGet("{id:int}/roadmap")]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    [ProducesResponseType(typeof(IReadOnlyList<RoadmapSkillNode>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoadmap(
        int id,
        [FromQuery] bool full = false,
        CancellationToken ct = default)
    {
        var result = await _roadmap.GetRoadmapAsync(id, full, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    // ── Seniority progress (#15) ──────────────────────────────────────────────

    /// <summary>
    /// Returns the current user's seniority progress ("You meet 14/18 Medior requirements").
    /// </summary>
    [HttpGet("me/seniority-progress")]
    [ProducesResponseType(typeof(SeniorityProgressResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetMySeniorityProgress(CancellationToken ct = default)
        => GetSeniorityProgressForUserAsync(GetCurrentUserId(), ct);

    /// <summary>
    /// Returns a consultant's seniority progress (manager/backoffice view).
    /// </summary>
    [HttpGet("{id:int}/seniority-progress")]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    [ProducesResponseType(typeof(SeniorityProgressResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSeniorityProgress(int id, CancellationToken ct = default)
    {
        var result = await _roadmap.GetSeniorityProgressAsync(id, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    // ── Goals (#18) ───────────────────────────────────────────────────────────

    /// <summary>Returns the current user's goals.</summary>
    [HttpGet("me/goals")]
    [ProducesResponseType(typeof(IReadOnlyList<GoalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyGoals(CancellationToken ct = default)
    {
        var consultantId = await ResolveConsultantIdAsync(GetCurrentUserId(), ct);
        if (consultantId is null) return NotFound();
        var result = await _goals.GetGoalsForConsultantAsync(consultantId.Value, ct);
        return Ok(result);
    }

    /// <summary>Returns all goals for a specific consultant (coach/backoffice view).</summary>
    [HttpGet("{id:int}/goals")]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    [ProducesResponseType(typeof(IReadOnlyList<GoalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGoals(int id, CancellationToken ct = default)
    {
        var result = await _goals.GetGoalsForConsultantAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Creates a coaching goal for a consultant (coach/backoffice only).</summary>
    [HttpPost("{id:int}/goals")]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    [ProducesResponseType(typeof(GoalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateGoal(
        int id,
        [FromBody] CreateGoalRequest request,
        CancellationToken ct = default)
    {
        var coachUserId = GetCurrentUserId();
        var result = await _goals.CreateGoalAsync(id, request, coachUserId, ct);
        if (result is null) return NotFound();
        return CreatedAtAction(nameof(GetGoals), new { id }, result);
    }

    // ── Readiness flags (#20) ─────────────────────────────────────────────────

    /// <summary>Returns all active readiness flags for a consultant's goals (coach dashboard).</summary>
    [HttpGet("{id:int}/readiness-flags")]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    [ProducesResponseType(typeof(IReadOnlyList<ConsultantReadinessFlagDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReadinessFlags(int id, CancellationToken ct = default)
    {
        var result = await _flags.GetActiveFlagsForConsultantAsync(id, ct);
        return Ok(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string GetCurrentUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? string.Empty;

    private async Task<IActionResult> GetRoadmapForUserAsync(
        string userId, bool full, CancellationToken ct)
    {
        var consultantId = await ResolveConsultantIdAsync(userId, ct);
        if (consultantId is null) return NotFound();

        var result = await _roadmap.GetRoadmapAsync(consultantId.Value, full, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    private async Task<IActionResult> GetSeniorityProgressForUserAsync(
        string userId, CancellationToken ct)
    {
        var consultantId = await ResolveConsultantIdAsync(userId, ct);
        if (consultantId is null) return NotFound();

        var result = await _roadmap.GetSeniorityProgressAsync(consultantId.Value, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    private async Task<int?> ResolveConsultantIdAsync(string userId, CancellationToken ct)
    {
        var id = await _db.Consultants
            .Where(c => c.UserId == userId)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync(ct);
        return id;
    }
}
