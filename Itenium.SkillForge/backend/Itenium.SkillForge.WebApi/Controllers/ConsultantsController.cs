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

    public ConsultantsController(IProfileService profiles)
    {
        _profiles = profiles;
    }

    /// <summary>
    /// Assigns (or clears) the competence centre profile for a consultant.
    /// Pass <c>null</c> as <see cref="AssignProfileRequest.ProfileId"/> to remove the current profile.
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
        var success = await _profiles.AssignProfileToConsultantAsync(id, request.ProfileId, ct);
        if (!success) return NotFound();
        return NoContent();
    }
}
