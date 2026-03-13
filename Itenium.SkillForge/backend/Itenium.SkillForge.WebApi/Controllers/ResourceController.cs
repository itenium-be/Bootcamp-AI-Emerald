using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Resources;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResourceController(AppDbContext db, ISkillForgeUser user) : ControllerBase
{
    private readonly AppDbContext _db = db;
    private readonly ISkillForgeUser _user = user;

    /// <summary>Browse resources, optionally filtered by skillId.</summary>
    [HttpGet]
    public async Task<ActionResult<List<ResourceEntity>>> GetResources([FromQuery] int? skillId)
    {
        var query = _db.Resources.AsQueryable();
        if (skillId.HasValue)
            query = query.Where(r => r.SkillId == skillId.Value);
        return Ok(await query.ToListAsync());
    }

    /// <summary>Create a resource (any authenticated user).</summary>
    [HttpPost]
    public async Task<ActionResult<ResourceEntity>> CreateResource([FromBody] CreateResourceRequest request)
    {
        var resource = new ResourceEntity
        {
            Title = request.Title,
            Url = request.Url,
            Type = request.Type,
            SkillId = request.SkillId,
            FromNiveau = request.FromNiveau,
            ToNiveau = request.ToNiveau,
            AddedByUserId = _user.UserId ?? string.Empty,
        };
        _db.Resources.Add(resource);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetResources), new { }, resource);
    }

    /// <summary>Delete a resource (only the owner or backoffice).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteResource(int id)
    {
        var resource = await _db.Resources.FindAsync(id);
        if (resource == null) return NotFound();
        if (!_user.IsBackOffice && resource.AddedByUserId != _user.UserId) return Forbid();

        _db.Resources.Remove(resource);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Mark a resource as complete (one per user per resource).</summary>
    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> MarkComplete(int id)
    {
        var resource = await _db.Resources.FindAsync(id);
        if (resource == null) return NotFound();

        var userId = _user.UserId ?? string.Empty;
        var existing = await _db.ResourceCompletions
            .AnyAsync(rc => rc.ResourceId == id && rc.UserId == userId);
        if (existing)
            return BadRequest("Resource already marked as complete.");

        _db.ResourceCompletions.Add(new ResourceCompletionEntity { ResourceId = id, UserId = userId });
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Unmark a resource completion.</summary>
    [HttpDelete("{id:int}/complete")]
    public async Task<IActionResult> UnmarkComplete(int id)
    {
        var userId = _user.UserId ?? string.Empty;
        var completion = await _db.ResourceCompletions
            .FirstOrDefaultAsync(rc => rc.ResourceId == id && rc.UserId == userId);
        if (completion == null) return NotFound();

        _db.ResourceCompletions.Remove(completion);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Rate a resource — creates or updates an existing rating.</summary>
    [HttpPost("{id:int}/rate")]
    public async Task<IActionResult> RateResource(int id, [FromBody] RateResourceRequest request)
    {
        var resource = await _db.Resources.FindAsync(id);
        if (resource == null) return NotFound();

        var userId = _user.UserId ?? string.Empty;
        var rating = await _db.ResourceRatings
            .FirstOrDefaultAsync(r => r.ResourceId == id && r.UserId == userId);

        if (rating == null)
        {
            _db.ResourceRatings.Add(new ResourceRatingEntity
            {
                ResourceId = id,
                UserId = userId,
                IsPositive = request.IsPositive,
            });
        }
        else
        {
            rating.IsPositive = request.IsPositive;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateResourceRequest(string Title, string Url, ResourceType Type, int SkillId, int FromNiveau, int ToNiveau);
public record RateResourceRequest(bool IsPositive);
