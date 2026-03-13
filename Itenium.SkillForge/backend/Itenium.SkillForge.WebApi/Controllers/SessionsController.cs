using System.Security.Claims;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.Services.Coaching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly ICoachingSessionService _sessions;
    private readonly ITeamQueryScope _scope;

    public SessionsController(ICoachingSessionService sessions, ITeamQueryScope scope)
    {
        _sessions = sessions;
        _scope = scope;
    }

    // ── Start session ─────────────────────────────────────────────────────────

    /// <summary>Starts a new coaching session for a consultant.</summary>
    [HttpPost("api/consultants/{id:int}/sessions")]
    [Authorize(Policy = SkillForgePolicies.Manager)]
    [ProducesResponseType(typeof(CoachingSessionRecord), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartSession(int id, CancellationToken ct = default)
    {
        var coachUserId = GetCurrentUserId();
        var result = await _sessions.StartSessionAsync(id, coachUserId, _scope, ct);
        if (result is null) return NotFound();
        return CreatedAtAction(nameof(GetSessions), new { id }, result);
    }

    // ── Update notes ──────────────────────────────────────────────────────────

    /// <summary>Updates free-text notes on an existing session.</summary>
    [HttpPut("api/sessions/{sessionId:int}/notes")]
    [Authorize(Policy = SkillForgePolicies.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNotes(int sessionId, [FromBody] UpdateSessionNotesRequest request, CancellationToken ct = default)
    {
        var success = await _sessions.UpdateNotesAsync(sessionId, request.Notes, ct);
        if (!success) return NotFound();
        return NoContent();
    }

    // ── Close session ─────────────────────────────────────────────────────────

    /// <summary>Closes a session by recording ClosedAt.</summary>
    [HttpPost("api/sessions/{sessionId:int}/close")]
    [Authorize(Policy = SkillForgePolicies.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloseSession(int sessionId, CancellationToken ct = default)
    {
        var success = await _sessions.CloseSessionAsync(sessionId, ct);
        if (!success) return NotFound();
        return NoContent();
    }

    // ── Session history ───────────────────────────────────────────────────────

    /// <summary>Returns all coaching sessions for a consultant, newest first.</summary>
    [HttpGet("api/consultants/{id:int}/sessions")]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    [ProducesResponseType(typeof(IReadOnlyList<CoachingSessionRecord>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessions(int id, CancellationToken ct = default)
    {
        var result = await _sessions.GetSessionsAsync(id, _scope, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    private string GetCurrentUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? string.Empty;
}
