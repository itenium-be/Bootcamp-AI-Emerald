using Itenium.SkillForge.Services;
using Itenium.SkillForge.Services.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConsultantsController : ControllerBase
{
    private readonly IProfileService _profiles;
    private readonly ITeamQueryScope _scope;

    public ConsultantsController(IProfileService profiles, ITeamQueryScope scope)
    {
        _profiles = profiles;
        _scope = scope;
    }

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
}
