using System.Security.Claims;
using Itenium.SkillForge.Entities.Resources;
using Itenium.SkillForge.Services.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/resources")]
[Authorize]
public class ResourcesController : ControllerBase
{
    private readonly IResourceService _resources;

    public ResourcesController(IResourceService resources) => _resources = resources;

    /// <summary>
    /// Returns resources, optionally filtered by skill, type, and niveau range.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ResourceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResources(
        [FromQuery] int? skillId = null,
        [FromQuery] ResourceType? type = null,
        [FromQuery] int? fromNiveau = null,
        [FromQuery] int? toNiveau = null,
        CancellationToken ct = default)
    {
        var result = await _resources.GetResourcesAsync(skillId, type, fromNiveau, toNiveau, ct);
        return Ok(result);
    }

    /// <summary>Creates a new resource contributed by the current user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateResource(
        [FromBody] CreateResourceRequest request,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _resources.CreateResourceAsync(request, userId, ct);
        return CreatedAtAction(nameof(GetResources), result);
    }

    /// <summary>
    /// Marks a resource as completed for the current user (idempotent).
    /// </summary>
    [HttpPost("{id:int}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompleteResource(int id, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        await _resources.CompleteResourceAsync(id, userId, ct);
        return NoContent();
    }

    /// <summary>
    /// Adds or updates a thumbs-up/down rating for the current user.
    /// </summary>
    [HttpPost("{id:int}/rate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RateResource(
        int id,
        [FromBody] RateResourceRequest request,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        await _resources.RateResourceAsync(id, userId, request.IsPositive, ct);
        return NoContent();
    }

    private string GetCurrentUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? string.Empty;
}

public record RateResourceRequest(bool IsPositive);
