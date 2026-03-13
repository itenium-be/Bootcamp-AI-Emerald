using Itenium.SkillForge.Entities.Profiles;
using Itenium.SkillForge.Entities.Skills;
using Itenium.SkillForge.Services.Import;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.Data;

/// <summary>
/// EF Core implementation of <see cref="ISkillCatalogueImporter"/>.
/// Upserts all entities in dependency order; idempotent on re-run.
/// </summary>
internal sealed class SkillCatalogueImporter : ISkillCatalogueImporter
{
    private readonly AppDbContext _db;

    public SkillCatalogueImporter(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SkillImportResult> ImportAsync(ParsedCatalogue catalogue, CancellationToken ct = default)
    {
        var categoriesCreated = await ImportCategoriesAsync(catalogue, ct);
        var skillsCreated = await ImportSkillsAsync(catalogue, ct);
        var levelsCreated = await ImportLevelsAsync(catalogue, ct);
        var prerequisitesCreated = await ImportPrerequisitesAsync(catalogue, ct);
        var (profilesCreated, profileSkillsCreated) = await ImportProfilesAndSkillsAsync(catalogue, ct);
        var thresholdsCreated = await ImportThresholdsAsync(catalogue, ct);

        return new SkillImportResult(
            CategoriesCreated: categoriesCreated,
            SkillsCreated: skillsCreated,
            LevelsCreated: levelsCreated,
            PrerequisitesCreated: prerequisitesCreated,
            ProfilesCreated: profilesCreated,
            ProfileSkillsCreated: profileSkillsCreated,
            ThresholdsCreated: thresholdsCreated);
    }

    // ── Categories ────────────────────────────────────────────────────────────

    private async Task<int> ImportCategoriesAsync(ParsedCatalogue catalogue, CancellationToken ct)
    {
        var existing = await _db.SkillCategories
            .ToDictionaryAsync(c => c.Name, ct);

        var toCreate = catalogue.Skills
            .Select(s => s.Category)
            .Distinct(StringComparer.Ordinal)
            .Where(name => !existing.ContainsKey(name))
            .ToList();

        foreach (var name in toCreate)
            _db.SkillCategories.Add(new SkillCategoryEntity { Name = name });

        await _db.SaveChangesAsync(ct);
        return toCreate.Count;
    }

    // ── Skills ────────────────────────────────────────────────────────────────

    private async Task<int> ImportSkillsAsync(ParsedCatalogue catalogue, CancellationToken ct)
    {
        var existingCategories = await _db.SkillCategories
            .ToDictionaryAsync(c => c.Name, ct);

        var existingSkills = await _db.Skills
            .Select(s => s.Name)
            .ToHashSetAsync(ct);

        var toCreate = catalogue.Skills
            .Where(row => !existingSkills.Contains(row.Name))
            .ToList();

        foreach (var row in toCreate)
        {
            _db.Skills.Add(new SkillEntity
            {
                Name = row.Name,
                Category = existingCategories[row.Category],
                Description = string.IsNullOrEmpty(row.Description) ? null : row.Description,
                LevelCount = row.LevelCount,
            });
        }

        await _db.SaveChangesAsync(ct);
        return toCreate.Count;
    }

    // ── Levels ────────────────────────────────────────────────────────────────

    private async Task<int> ImportLevelsAsync(ParsedCatalogue catalogue, CancellationToken ct)
    {
        var skillsByName = await _db.Skills
            .ToDictionaryAsync(s => s.Name, ct);

        var existingKeys = (await _db.SkillLevels
            .Select(l => new { l.SkillId, l.Niveau })
            .ToListAsync(ct))
            .Select(l => (l.SkillId, l.Niveau))
            .ToHashSet();

        var created = 0;
        foreach (var row in catalogue.Levels)
        {
            if (!skillsByName.TryGetValue(row.SkillName, out var skill)) continue;
            if (!existingKeys.Add((skill.Id, row.Niveau))) continue;

            _db.SkillLevels.Add(new SkillLevelEntity
            {
                Skill = skill,
                Niveau = row.Niveau,
                Descriptor = row.Descriptor,
            });
            created++;
        }

        await _db.SaveChangesAsync(ct);
        return created;
    }

    // ── Prerequisites ─────────────────────────────────────────────────────────

    private async Task<int> ImportPrerequisitesAsync(ParsedCatalogue catalogue, CancellationToken ct)
    {
        var skillsByName = await _db.Skills
            .ToDictionaryAsync(s => s.Name, ct);

        var existingKeys = (await _db.SkillPrerequisites
            .Select(p => new { p.SkillId, p.RequiredSkillId })
            .ToListAsync(ct))
            .Select(p => (p.SkillId, p.RequiredSkillId))
            .ToHashSet();

        var created = 0;
        foreach (var row in catalogue.Prerequisites)
        {
            if (!skillsByName.TryGetValue(row.SkillName, out var skill)) continue;
            if (!skillsByName.TryGetValue(row.RequiredSkillName, out var required)) continue;
            if (!existingKeys.Add((skill.Id, required.Id))) continue;

            _db.SkillPrerequisites.Add(new SkillPrerequisiteEntity
            {
                Skill = skill,
                RequiredSkill = required,
                RequiredMinNiveau = row.RequiredMinNiveau,
            });
            created++;
        }

        await _db.SaveChangesAsync(ct);
        return created;
    }

    // ── Profiles + profile-skill mappings ────────────────────────────────────

    private async Task<(int ProfilesCreated, int ProfileSkillsCreated)> ImportProfilesAndSkillsAsync(
        ParsedCatalogue catalogue, CancellationToken ct)
    {
        var skillsByName = await _db.Skills
            .ToDictionaryAsync(s => s.Name, ct);

        var existingProfiles = await _db.CompetenceCentreProfiles
            .ToDictionaryAsync(p => p.Name, ct);

        // Collect all profile names referenced in catalogue
        var allProfileNames = catalogue.ProfileSkills.Select(ps => ps.ProfileName)
            .Concat(catalogue.SeniorityThresholds.Select(t => t.ProfileName))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var newProfileNames = allProfileNames
            .Where(name => !existingProfiles.ContainsKey(name))
            .ToList();

        foreach (var name in newProfileNames)
        {
            var entity = new CompetenceCentreProfileEntity { Name = name };
            _db.CompetenceCentreProfiles.Add(entity);
            existingProfiles[name] = entity;
        }

        await _db.SaveChangesAsync(ct);

        // Profile-skill mappings
        var existingProfileSkillKeys = (await _db.ProfileSkills
            .Select(ps => new { ps.ProfileId, ps.SkillId })
            .ToListAsync(ct))
            .Select(ps => (ps.ProfileId, ps.SkillId))
            .ToHashSet();

        var profileSkillsCreated = 0;
        foreach (var row in catalogue.ProfileSkills)
        {
            if (!existingProfiles.TryGetValue(row.ProfileName, out var profile)) continue;
            if (!skillsByName.TryGetValue(row.SkillName, out var skill)) continue;
            if (!existingProfileSkillKeys.Add((profile.Id, skill.Id))) continue;

            _db.ProfileSkills.Add(new ProfileSkillEntity
            {
                Profile = profile,
                Skill = skill,
            });
            profileSkillsCreated++;
        }

        await _db.SaveChangesAsync(ct);

        return (newProfileNames.Count, profileSkillsCreated);
    }

    // ── Seniority thresholds ──────────────────────────────────────────────────

    private async Task<int> ImportThresholdsAsync(ParsedCatalogue catalogue, CancellationToken ct)
    {
        var skillsByName = await _db.Skills
            .ToDictionaryAsync(s => s.Name, ct);

        var profilesByName = await _db.CompetenceCentreProfiles
            .ToDictionaryAsync(p => p.Name, ct);

        var existingKeys = (await _db.SeniorityThresholds
            .Select(t => new { t.ProfileId, t.SeniorityLevel, t.SkillId })
            .ToListAsync(ct))
            .Select(t => (t.ProfileId, t.SeniorityLevel, t.SkillId))
            .ToHashSet();

        var created = 0;
        foreach (var row in catalogue.SeniorityThresholds)
        {
            if (!profilesByName.TryGetValue(row.ProfileName, out var profile)) continue;
            if (!skillsByName.TryGetValue(row.SkillName, out var skill)) continue;
            if (!existingKeys.Add((profile.Id, row.SeniorityLevel, skill.Id))) continue;

            _db.SeniorityThresholds.Add(new SeniorityThresholdEntity
            {
                Profile = profile,
                Skill = skill,
                SeniorityLevel = row.SeniorityLevel,
                MinNiveau = row.MinNiveau,
            });
            created++;
        }

        await _db.SaveChangesAsync(ct);
        return created;
    }
}
