using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.Entities.Profiles;
using Itenium.SkillForge.Entities.Skills;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class SkillCatalogueSchemaTests : DatabaseTestBase
{
    [Test]
    public async Task SkillCategory_CanBeSavedAndRetrieved()
    {
        var category = new SkillCategoryEntity { Name = "Clean Code" };
        Db.SkillCategories.Add(category);
        await Db.SaveChangesAsync();

        var saved = await Db.SkillCategories.FindAsync(category.Id);

        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.Name, Is.EqualTo("Clean Code"));
    }

    [Test]
    public async Task Skill_CanBeSavedWithLevelsAndCategory()
    {
        var category = new SkillCategoryEntity { Name = "Software Design" };
        Db.SkillCategories.Add(category);
        var skill = new SkillEntity
        {
            Name = "Clean Code",
            Category = category,
            Description = "Write readable, maintainable code",
            LevelCount = 5,
        };
        skill.Levels.Add(new SkillLevelEntity { Niveau = 1, Descriptor = "Applies basic naming conventions" });
        skill.Levels.Add(new SkillLevelEntity { Niveau = 2, Descriptor = "Consistently writes readable methods" });
        Db.Skills.Add(skill);
        await Db.SaveChangesAsync();

        var saved = await Db.Skills
            .Include(s => s.Category)
            .Include(s => s.Levels)
            .FirstAsync(s => s.Id == skill.Id);

        Assert.That(saved.Name, Is.EqualTo("Clean Code"));
        Assert.That(saved.LevelCount, Is.EqualTo(5));
        Assert.That(saved.Category.Name, Is.EqualTo("Software Design"));
        Assert.That(saved.Levels, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Skill_CanHavePrerequisites()
    {
        var category = new SkillCategoryEntity { Name = "Architecture" };
        Db.SkillCategories.Add(category);
        var cleanCode = new SkillEntity { Name = "Clean Code", Category = category, LevelCount = 3 };
        var ddd = new SkillEntity { Name = "Domain-Driven Design", Category = category, LevelCount = 3 };
        Db.Skills.AddRange(cleanCode, ddd);
        await Db.SaveChangesAsync();

        Db.SkillPrerequisites.Add(new SkillPrerequisiteEntity
        {
            SkillId = ddd.Id,
            RequiredSkillId = cleanCode.Id,
            RequiredMinNiveau = 3,
        });
        await Db.SaveChangesAsync();

        var saved = await Db.SkillPrerequisites
            .Include(p => p.RequiredSkill)
            .FirstAsync(p => p.SkillId == ddd.Id);

        Assert.That(saved.RequiredSkill.Name, Is.EqualTo("Clean Code"));
        Assert.That(saved.RequiredMinNiveau, Is.EqualTo(3));
    }

    [Test]
    public async Task CompetenceCentreProfile_CanHaveSkillsAndSeniorityThresholds()
    {
        var category = new SkillCategoryEntity { Name = "Backend" };
        Db.SkillCategories.Add(category);
        var skill = new SkillEntity { Name = "Entity Framework", Category = category, LevelCount = 4 };
        Db.Skills.Add(skill);
        var profile = new CompetenceCentreProfileEntity { Name = ".NET" };
        Db.CompetenceCentreProfiles.Add(profile);
        await Db.SaveChangesAsync();

        Db.ProfileSkills.Add(new ProfileSkillEntity { ProfileId = profile.Id, SkillId = skill.Id });
        Db.SeniorityThresholds.Add(new SeniorityThresholdEntity
        {
            ProfileId = profile.Id,
            SkillId = skill.Id,
            SeniorityLevel = SeniorityLevel.Medior,
            MinNiveau = 2,
        });
        await Db.SaveChangesAsync();

        var savedProfile = await Db.CompetenceCentreProfiles
            .Include(p => p.ProfileSkills)
            .Include(p => p.SeniorityThresholds)
            .FirstAsync(p => p.Id == profile.Id);

        Assert.That(savedProfile.ProfileSkills, Has.Count.EqualTo(1));
        Assert.That(savedProfile.SeniorityThresholds, Has.Count.EqualTo(1));
        Assert.That(savedProfile.SeniorityThresholds.First().MinNiveau, Is.EqualTo(2));
    }

    [Test]
    public async Task Consultant_CanBeAssignedToCompetenceCentreProfile()
    {
        var profile = new CompetenceCentreProfileEntity { Name = "Java" };
        Db.CompetenceCentreProfiles.Add(profile);
        await Db.SaveChangesAsync();

        var consultant = new ConsultantEntity { UserId = "user-lea-123", ProfileId = profile.Id };
        Db.Consultants.Add(consultant);
        await Db.SaveChangesAsync();

        var saved = await Db.Consultants
            .Include(c => c.Profile)
            .FirstAsync(c => c.UserId == "user-lea-123");

        Assert.That(saved.Profile!.Name, Is.EqualTo("Java"));
        Assert.That(saved.IsArchived, Is.False);
    }

    [Test]
    public async Task Consultant_CanBeArchived_WhilePreservingRecord()
    {
        var consultant = new ConsultantEntity { UserId = "user-archived-456" };
        Db.Consultants.Add(consultant);
        await Db.SaveChangesAsync();

        consultant.ArchivedAt = DateTime.UtcNow;
        await Db.SaveChangesAsync();

        var saved = await Db.Consultants.FindAsync(consultant.Id);

        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.IsArchived, Is.True);
        Assert.That(saved.ArchivedAt, Is.Not.Null);
    }

    [Test]
    public async Task Consultant_UserId_IsUnique()
    {
        Db.Consultants.Add(new ConsultantEntity { UserId = "user-duplicate" });
        await Db.SaveChangesAsync();

        Db.Consultants.Add(new ConsultantEntity { UserId = "user-duplicate" });

        Assert.That(
            async () => await Db.SaveChangesAsync(),
            Throws.Exception,
            "Inserting a second ConsultantEntity with the same UserId should violate the unique index");
    }
}
