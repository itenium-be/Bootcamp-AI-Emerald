using Itenium.SkillForge.Services;
using Itenium.SkillForge.Services.Coaching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/coach")]
[Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
public class CoachDashboardController : ControllerBase
{
    private readonly ICoachDashboardService _dashboard;
    private readonly ITeamQueryScope _scope;

    public CoachDashboardController(ICoachDashboardService dashboard, ITeamQueryScope scope)
    {
        _dashboard = dashboard;
        _scope = scope;
    }

    /// <summary>
    /// Team overview for coach: active goals, readiness flags, inactivity, overdue goals.
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(IReadOnlyList<CoachDashboardRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct = default)
    {
        var rows = await _dashboard.GetDashboardAsync(_scope, ct);
        return Ok(rows);
    }

    /// <summary>
    /// Full activity history for a consultant: resources, goals, validations, flags, sessions.
    /// </summary>
    [HttpGet("consultants/{id:int}/activity")]
    [ProducesResponseType(typeof(ConsultantActivityHistory), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActivity(int id, CancellationToken ct = default)
    {
        var result = await _dashboard.GetActivityAsync(id, _scope, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }
}
