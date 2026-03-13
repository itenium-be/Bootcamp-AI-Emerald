using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/coaching-sessions")]
[Authorize]
public class CoachingSessionController(AppDbContext db, ITeamQueryScope scope, ISkillForgeUser user) : ControllerBase
{
    private readonly AppDbContext _db = db;
    private readonly ITeamQueryScope _scope = scope;
    private readonly ISkillForgeUser _user = user;

    /// <summary>Start a coaching session (manager).</summary>
    [HttpPost]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    public async Task<ActionResult<CoachingSessionEntity>> StartSession([FromBody] StartSessionRequest request)
    {
        var consultant = await _db.Consultants.FirstOrDefaultAsync(c => c.UserId == request.ConsultantUserId);
        if (consultant == null) return NotFound();
        if (!_scope.IsBackOffice && !_scope.TeamIds.Contains(consultant.TeamId)) return Forbid();

        var session = new CoachingSessionEntity
        {
            ConsultantUserId = request.ConsultantUserId,
            CoachUserId = _user.UserId ?? string.Empty,
        };
        _db.CoachingSessions.Add(session);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSessions), new { consultantUserId = request.ConsultantUserId }, session);
    }

    /// <summary>Close a coaching session with optional notes (manager).</summary>
    [HttpPost("{id:int}/close")]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    public async Task<IActionResult> CloseSession(int id, [FromBody] CloseSessionRequest request)
    {
        var session = await _db.CoachingSessions.FindAsync(id);
        if (session == null) return NotFound();
        if (session.ClosedAt.HasValue) return BadRequest("Session is already closed.");

        session.ClosedAt = DateTime.UtcNow;
        session.Notes = request.Notes;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Get sessions for a consultant. Managers see full list; learners only see their own.</summary>
    [HttpGet]
    public async Task<ActionResult<List<CoachingSessionEntity>>> GetSessions([FromQuery] string? consultantUserId = null)
    {
        var userId = _user.UserId ?? string.Empty;
        var isManager = _scope.IsBackOffice || _scope.TeamIds.Count > 0;

        var query = _db.CoachingSessions.AsQueryable();

        if (consultantUserId != null)
        {
            // If learner requesting their own sessions — allow. If someone else's, verify team access.
            if (!isManager && consultantUserId != userId)
                return Forbid();

            if (isManager && consultantUserId != userId)
            {
                // Verify team access
                var consultant = await _db.Consultants.FirstOrDefaultAsync(c => c.UserId == consultantUserId);
                if (consultant != null && !_scope.IsBackOffice && !_scope.TeamIds.Contains(consultant.TeamId))
                    return Forbid();
            }

            query = query.Where(s => s.ConsultantUserId == consultantUserId);
        }
        else if (!isManager)
        {
            // Learner with no consultantUserId param — return own sessions
            query = query.Where(s => s.ConsultantUserId == userId);
        }

        return Ok(await query.ToListAsync());
    }
}

public record StartSessionRequest(string ConsultantUserId);
public record CloseSessionRequest(string? Notes);
