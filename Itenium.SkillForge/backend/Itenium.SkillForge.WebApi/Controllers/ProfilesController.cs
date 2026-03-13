using Itenium.SkillForge.Services.Profiles;
using Itenium.SkillForge.Services.SkillCatalogue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfilesController : ControllerBase
{
    private readonly IProfileService _profiles;

    public ProfilesController(IProfileService profiles)
    {
        _profiles = profiles;
    }

    /// <summary>Returns all competence centre profiles, ordered by name.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProfileListItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProfileListItem>>> GetProfiles(
        CancellationToken ct = default)
    {
        var profiles = await _profiles.GetProfilesAsync(ct);
        return Ok(profiles);
    }

    /// <summary>
    /// Returns all skills belonging to a profile, ordered by category then name.
    /// </summary>
    [HttpGet("{id:int}/skills")]
    [ProducesResponseType<IReadOnlyList<SkillListItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SkillListItem>>> GetProfileSkills(
        int id,
        CancellationToken ct = default)
    {
        var skills = await _profiles.GetProfileSkillsAsync(id, ct);
        if (skills is null) return NotFound();
        return Ok(skills);
    }

    /// <summary>
    /// Returns seniority thresholds for the profile grouped by seniority level.
    /// </summary>
    [HttpGet("{id:int}/seniority-thresholds")]
    [ProducesResponseType<IReadOnlyList<SeniorityThresholdsForLevel>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SeniorityThresholdsForLevel>>> GetSeniorityThresholds(
        int id,
        CancellationToken ct = default)
    {
        var thresholds = await _profiles.GetSeniorityThresholdsAsync(id, ct);
        if (thresholds is null) return NotFound();
        return Ok(thresholds);
    }
}
