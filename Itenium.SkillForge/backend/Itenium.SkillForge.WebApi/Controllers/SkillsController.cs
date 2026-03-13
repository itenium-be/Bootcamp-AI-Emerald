using Itenium.SkillForge.Services.SkillCatalogue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SkillsController : ControllerBase
{
    private readonly ISkillCatalogueService _catalogue;

    public SkillsController(ISkillCatalogueService catalogue)
    {
        _catalogue = catalogue;
    }

    /// <summary>
    /// Returns all skills in the catalogue.
    /// Optionally filter by <paramref name="categoryId"/> or <paramref name="profileId"/>.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SkillListItem>>> GetSkills(
        [FromQuery] int? categoryId = null,
        [FromQuery] int? profileId = null,
        CancellationToken ct = default)
    {
        var skills = await _catalogue.GetSkillsAsync(categoryId, profileId, ct);
        return Ok(skills);
    }

    /// <summary>
    /// Returns full detail of a skill including level descriptors and prerequisites.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SkillDetail>> GetSkill(int id, CancellationToken ct = default)
    {
        var skill = await _catalogue.GetSkillDetailAsync(id, ct);
        if (skill is null) return NotFound();
        return Ok(skill);
    }
}
