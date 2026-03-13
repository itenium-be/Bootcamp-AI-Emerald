using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/skill-validations")]
[Authorize]
public class SkillValidationController(AppDbContext db, ITeamQueryScope scope, ISkillForgeUser user) : ControllerBase
{
    private readonly AppDbContext _db = db;
    private readonly ITeamQueryScope _scope = scope;
    private readonly ISkillForgeUser _user = user;

    /// <summary>Validate a skill niveau for a consultant (immutable record).</summary>
    [HttpPost]
    [Authorize(Policy = SkillForgePolicies.ManagerOrBackoffice)]
    public async Task<ActionResult<SkillValidationEntity>> ValidateSkill([FromBody] ValidateSkillRequest request)
    {
        var consultant = await _db.Consultants.FirstOrDefaultAsync(c => c.UserId == request.ConsultantUserId);
        if (consultant == null) return NotFound();
        if (!_scope.IsBackOffice && !_scope.TeamIds.Contains(consultant.TeamId)) return Forbid();

        var validation = new SkillValidationEntity
        {
            ConsultantUserId = request.ConsultantUserId,
            CoachUserId = _user.UserId ?? string.Empty,
            SkillId = request.SkillId,
            Niveau = request.Niveau,
            SessionId = request.SessionId,
        };
        _db.SkillValidations.Add(validation);

        // Update or create ConsultantSkillEntity
        var consultantSkill = await _db.ConsultantSkills
            .FirstOrDefaultAsync(cs => cs.ConsultantId == request.ConsultantUserId && cs.SkillId == request.SkillId);
        if (consultantSkill == null)
        {
            _db.ConsultantSkills.Add(new ConsultantSkillEntity
            {
                ConsultantId = request.ConsultantUserId,
                SkillId = request.SkillId,
                CurrentLevel = request.Niveau,
            });
        }
        else
        {
            consultantSkill.CurrentLevel = request.Niveau;
        }

        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetValidations), new { consultantUserId = request.ConsultantUserId }, validation);
    }

    /// <summary>Get validation history for a consultant.</summary>
    [HttpGet]
    public async Task<ActionResult<List<SkillValidationEntity>>> GetValidations([FromQuery] string? consultantUserId = null)
    {
        var userId = _user.UserId ?? string.Empty;
        var isManager = _scope.IsBackOffice || _scope.TeamIds.Count > 0;

        var query = _db.SkillValidations.AsQueryable();

        if (consultantUserId != null)
        {
            // Learner can only see their own
            if (!isManager && consultantUserId != userId)
                return Forbid();

            query = query.Where(v => v.ConsultantUserId == consultantUserId);
        }
        else if (!isManager)
        {
            query = query.Where(v => v.ConsultantUserId == userId);
        }

        return Ok(await query.ToListAsync());
    }
}

public record ValidateSkillRequest(string ConsultantUserId, int SkillId, int Niveau, int? SessionId);
