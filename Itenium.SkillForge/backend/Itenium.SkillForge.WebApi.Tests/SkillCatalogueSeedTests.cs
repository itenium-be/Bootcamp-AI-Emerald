using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class SkillCatalogueSeedTests : DatabaseTestBase
{
    private static readonly int[] SevenLevels = [1, 2, 3, 4, 5, 6, 7];

    private static readonly string[] ExpectedCategories =
    [
        "Language & Runtime",
        "Web & API",
        "Data & Persistence",
        "Testing",
        "Architecture & Design",
        "Tooling & DevOps",
    ];

    private static readonly string[] DotNetCoreSkills = ["C#", "ASP.NET Core", "Entity Framework Core"];
    private static readonly string[] JavaCoreSkills = ["Java", "Spring Boot", "Hibernate / JPA"];
    private static readonly string[] SharedSkills = ["Clean Code", "Git", "Docker", "Testing Fundamentals"];
    private static readonly SeniorityLevel[] AllSeniorityLevels = [SeniorityLevel.Junior, SeniorityLevel.Medior, SeniorityLevel.Senior];

    [SetUp]
    public async Task SeedCatalogue()
    {
        await SkillCatalogueSeedData.Seed(Db);
    }

    [Test]
    public async Task Seed_CreatesExpectedCategories()
    {
        var categories = await Db.SkillCategories.Select(c => c.Name).ToListAsync();

        Assert.That(categories, Is.SupersetOf(ExpectedCategories));
    }

    [Test]
    public async Task Seed_DotNetProfile_Exists_WithCoreSkills()
    {
        var profile = await Db.CompetenceCentreProfiles
            .Include(p => p.ProfileSkills)
            .ThenInclude(ps => ps.Skill)
            .FirstAsync(p => p.Name == ".NET");

        var skillNames = profile.ProfileSkills.Select(ps => ps.Skill.Name).ToList();
        Assert.That(skillNames, Is.SupersetOf(DotNetCoreSkills));
    }

    [Test]
    public async Task Seed_JavaProfile_Exists_WithCoreSkills()
    {
        var profile = await Db.CompetenceCentreProfiles
            .Include(p => p.ProfileSkills)
            .ThenInclude(ps => ps.Skill)
            .FirstAsync(p => p.Name == "Java");

        var skillNames = profile.ProfileSkills.Select(ps => ps.Skill.Name).ToList();
        Assert.That(skillNames, Is.SupersetOf(JavaCoreSkills));
    }

    [Test]
    public async Task Seed_SharedSkills_AppearInBothProfiles()
    {
        var dotnet = await Db.CompetenceCentreProfiles
            .Include(p => p.ProfileSkills).ThenInclude(ps => ps.Skill)
            .FirstAsync(p => p.Name == ".NET");
        var java = await Db.CompetenceCentreProfiles
            .Include(p => p.ProfileSkills).ThenInclude(ps => ps.Skill)
            .FirstAsync(p => p.Name == "Java");

        var dotnetSkills = dotnet.ProfileSkills.Select(ps => ps.Skill.Name)
            .ToHashSet(StringComparer.Ordinal);
        var javaSkills = java.ProfileSkills.Select(ps => ps.Skill.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.That(dotnetSkills, Is.SupersetOf(SharedSkills));
        Assert.That(javaSkills, Is.SupersetOf(SharedSkills));
    }

    [Test]
    public async Task Seed_CSharp_HasSevenLevels()
    {
        var csharp = await Db.Skills
            .Include(s => s.Levels)
            .FirstAsync(s => s.Name == "C#");

        Assert.That(csharp.LevelCount, Is.EqualTo(7));
        Assert.That(csharp.Levels, Has.Count.EqualTo(7));
        Assert.That(csharp.Levels.Select(l => l.Niveau).Order().ToList(),
            Is.EqualTo(SevenLevels));
    }

    [Test]
    public async Task Seed_Java_HasSevenLevels()
    {
        var java = await Db.Skills
            .Include(s => s.Levels)
            .FirstAsync(s => s.Name == "Java");

        Assert.That(java.LevelCount, Is.EqualTo(7));
        Assert.That(java.Levels, Has.Count.EqualTo(7));
    }

    [Test]
    public async Task Seed_CheckboxSkill_HasLevelCountOne_AndNoLevelDescriptors()
    {
        var gitSkill = await Db.Skills
            .Include(s => s.Levels)
            .FirstAsync(s => s.Name == "Git");

        Assert.That(gitSkill.LevelCount, Is.EqualTo(1));
        Assert.That(gitSkill.Levels, Is.Empty);
    }

    [Test]
    public async Task Seed_Prerequisites_LinkAspNetCoreToCSharp()
    {
        var prereqs = await Db.SkillPrerequisites
            .Include(p => p.Skill)
            .Include(p => p.RequiredSkill)
            .Where(p => p.Skill.Name == "ASP.NET Core")
            .ToListAsync();

        Assert.That(prereqs, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(prereqs.Any(p => p.RequiredSkill.Name == "C#"), Is.True);
    }

    [Test]
    public async Task Seed_Prerequisites_LinkSpringBootToJava()
    {
        var prereqs = await Db.SkillPrerequisites
            .Include(p => p.Skill)
            .Include(p => p.RequiredSkill)
            .Where(p => p.Skill.Name == "Spring Boot")
            .ToListAsync();

        Assert.That(prereqs, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(prereqs.Any(p => p.RequiredSkill.Name == "Java"), Is.True);
    }

    [Test]
    public async Task Seed_SeniorityThresholds_DotNet_HasJuniorMediorSenior()
    {
        var profile = await Db.CompetenceCentreProfiles
            .Include(p => p.SeniorityThresholds)
            .FirstAsync(p => p.Name == ".NET");

        var levels = profile.SeniorityThresholds.Select(t => t.SeniorityLevel).Distinct().ToList();
        Assert.That(levels, Is.SupersetOf(AllSeniorityLevels));
    }

    [Test]
    public async Task Seed_SeniorityThresholds_Java_HasJuniorMediorSenior()
    {
        var profile = await Db.CompetenceCentreProfiles
            .Include(p => p.SeniorityThresholds)
            .FirstAsync(p => p.Name == "Java");

        var levels = profile.SeniorityThresholds.Select(t => t.SeniorityLevel).Distinct().ToList();
        Assert.That(levels, Is.SupersetOf(AllSeniorityLevels));
    }

    [Test]
    public async Task Seed_IsIdempotent_RunningTwiceDoesNotDuplicate()
    {
        await SkillCatalogueSeedData.Seed(Db); // second call

        var categoryCount = await Db.SkillCategories.CountAsync();
        var skillCount = await Db.Skills.CountAsync();
        var profileCount = await Db.CompetenceCentreProfiles.CountAsync();

        Assert.That(categoryCount, Is.EqualTo(6));
        Assert.That(skillCount, Is.GreaterThan(10));
        Assert.That(profileCount, Is.EqualTo(2));
    }
}
