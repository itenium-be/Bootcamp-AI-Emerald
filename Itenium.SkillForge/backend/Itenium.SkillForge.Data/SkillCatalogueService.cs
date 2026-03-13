using Itenium.SkillForge.Services.SkillCatalogue;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.Data;

/// <summary>
/// EF Core implementation of <see cref="ISkillCatalogueService"/>.
/// Lives in the infrastructure (Data) layer; consumed via the interface from WebApi.
/// </summary>
internal sealed class SkillCatalogueService : ISkillCatalogueService
{
    private readonly AppDbContext _db;

    public SkillCatalogueService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SkillListItem>> GetSkillsAsync(
        int? categoryId = null,
        int? profileId = null,
        CancellationToken ct = default)
    {
        var query = _db.Skills.AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(s => s.CategoryId == categoryId.Value);

        if (profileId.HasValue)
        {
            var profileSkillIds = _db.ProfileSkills
                .Where(ps => ps.ProfileId == profileId.Value)
                .Select(ps => ps.SkillId);
            query = query.Where(s => profileSkillIds.Contains(s.Id));
        }

        return await query
            .OrderBy(s => s.Category.Name)
            .ThenBy(s => s.Name)
            .Select(s => new SkillListItem(
                s.Id,
                s.Name,
                s.Category.Name,
                s.LevelCount,
                s.Description))
            .ToListAsync(ct);
    }

    public async Task<SkillDetail?> GetSkillDetailAsync(int id, CancellationToken ct = default)
    {
        var skill = await _db.Skills
            .Include(s => s.Category)
            .Include(s => s.Levels.OrderBy(l => l.Niveau))
            .Include(s => s.Prerequisites)
                .ThenInclude(p => p.RequiredSkill)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (skill is null) return null;

        return new SkillDetail(
            skill.Id,
            skill.Name,
            skill.Category.Name,
            skill.LevelCount,
            skill.Description,
            skill.Levels
                .OrderBy(l => l.Niveau)
                .Select(l => new SkillLevelDto(l.Niveau, l.Descriptor))
                .ToList(),
            skill.Prerequisites
                .Select(p => new SkillPrerequisiteDto(
                    p.RequiredSkillId,
                    p.RequiredSkill.Name,
                    p.RequiredMinNiveau))
                .ToList());
    }
}
