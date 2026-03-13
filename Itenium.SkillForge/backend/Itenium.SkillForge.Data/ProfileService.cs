using Itenium.SkillForge.Services;
using Itenium.SkillForge.Services.Profiles;
using Itenium.SkillForge.Services.SkillCatalogue;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.Data;

/// <summary>
/// EF Core implementation of <see cref="IProfileService"/>.
/// Lives in the infrastructure (Data) layer; consumed via the interface from WebApi.
/// </summary>
internal sealed class ProfileService : IProfileService
{
    private readonly AppDbContext _db;

    public ProfileService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ProfileListItem>> GetProfilesAsync(CancellationToken ct = default)
    {
        return await _db.CompetenceCentreProfiles
            .OrderBy(p => p.Name)
            .Select(p => new ProfileListItem(p.Id, p.Name))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SkillListItem>?> GetProfileSkillsAsync(
        int profileId,
        CancellationToken ct = default)
    {
        var profileExists = await _db.CompetenceCentreProfiles
            .AnyAsync(p => p.Id == profileId, ct);

        if (!profileExists) return null;

        return await _db.ProfileSkills
            .Where(ps => ps.ProfileId == profileId)
            .Select(ps => ps.Skill)
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

    public async Task<IReadOnlyList<SeniorityThresholdsForLevel>?> GetSeniorityThresholdsAsync(
        int profileId,
        CancellationToken ct = default)
    {
        var profileExists = await _db.CompetenceCentreProfiles
            .AnyAsync(p => p.Id == profileId, ct);

        if (!profileExists) return null;

        var thresholds = await _db.SeniorityThresholds
            .Where(t => t.ProfileId == profileId)
            .Include(t => t.Skill)
            .ToListAsync(ct);

        return thresholds
            .GroupBy(t => t.SeniorityLevel)
            .OrderBy(g => g.Key)
            .Select(g => new SeniorityThresholdsForLevel(
                g.Key,
                g.Select(t => new SeniorityThresholdDto(t.SkillId, t.Skill.Name, t.MinNiveau))
                    .OrderBy(t => t.SkillName, StringComparer.Ordinal)
                    .ToList()))
            .ToList();
    }

    public async Task<bool> AssignProfileToConsultantAsync(
        int consultantId,
        int? profileId,
        ITeamQueryScope scope,
        CancellationToken ct = default)
    {
        var consultant = await _db.Consultants
            .ApplyTeamScope(scope)
            .FirstOrDefaultAsync(c => c.Id == consultantId, ct);

        if (consultant is null) return false;

        consultant.ProfileId = profileId;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
