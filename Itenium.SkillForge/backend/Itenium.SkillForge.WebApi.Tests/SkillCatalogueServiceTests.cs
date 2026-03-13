using Itenium.SkillForge.Data;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Integration tests for <see cref="SkillCatalogueService"/> via its EF Core implementation.
/// Seed data is applied before each test and rolled back on teardown.
/// </summary>
[TestFixture]
public class SkillCatalogueServiceTests : DatabaseTestBase
{
    private static readonly string[] DotNetCoreSkills = ["C#", "ASP.NET Core", "Entity Framework Core"];
    private static readonly string[] JavaCoreSkills = ["Java", "Spring Boot", "Hibernate / JPA"];
    private static readonly int[] SevenLevels = [1, 2, 3, 4, 5, 6, 7];

    private SkillCatalogueService _service = null!;

    [SetUp]
    public async Task SetUp()
    {
        await SkillCatalogueSeedData.Seed(Db);
        _service = new SkillCatalogueService(Db);
    }

    [Test]
    public async Task GetSkills_ReturnsAllSkills_WhenNoFilterApplied()
    {
        var skills = await _service.GetSkillsAsync();

        Assert.That(skills, Is.Not.Empty);
        Assert.That(skills.Count, Is.GreaterThanOrEqualTo(20),
            "Expected at least 20 seeded skills");
    }

    [Test]
    public async Task GetSkills_FilteredByCategoryId_ReturnsOnlyThatCategory()
    {
        var langCategory = await Db.SkillCategories
            .FirstAsync(c => c.Name == "Language & Runtime");

        var skills = await _service.GetSkillsAsync(categoryId: langCategory.Id);

        Assert.That(skills, Is.Not.Empty);
        Assert.That(skills.All(s => s.CategoryName == "Language & Runtime"), Is.True);
    }

    [Test]
    public async Task GetSkills_FilteredByProfileId_ReturnsDotNetSkills()
    {
        var dotnetProfile = await Db.CompetenceCentreProfiles
            .FirstAsync(p => p.Name == ".NET");

        var skills = await _service.GetSkillsAsync(profileId: dotnetProfile.Id);

        var names = skills.Select(s => s.Name).ToHashSet(StringComparer.Ordinal);
        Assert.That(names, Is.SupersetOf(DotNetCoreSkills));
        Assert.That(names.Contains("Spring Boot", StringComparer.Ordinal), Is.False,
            "Java-specific skills should not appear in .NET profile filter");
    }

    [Test]
    public async Task GetSkills_FilteredByProfileId_ReturnsJavaSkills()
    {
        var javaProfile = await Db.CompetenceCentreProfiles
            .FirstAsync(p => p.Name == "Java");

        var skills = await _service.GetSkillsAsync(profileId: javaProfile.Id);

        var names = skills.Select(s => s.Name).ToHashSet(StringComparer.Ordinal);
        Assert.That(names, Is.SupersetOf(JavaCoreSkills));
        Assert.That(names.Contains("C#", StringComparer.Ordinal), Is.False,
            ".NET-specific skills should not appear in Java profile filter");
    }

    [Test]
    public async Task GetSkills_ResultsAreGroupedByCategory()
    {
        var skills = await _service.GetSkillsAsync();

        // Skills with the same category must appear in a contiguous block.
        // Exact sort order is DB-collation-dependent and not asserted here.
        var seenCategories = new HashSet<string>(StringComparer.Ordinal);
        string? currentCategory = null;

        foreach (var skill in skills)
        {
            if (!string.Equals(skill.CategoryName, currentCategory, StringComparison.Ordinal))
            {
                Assert.That(seenCategories.Add(skill.CategoryName), Is.True,
                    $"Category '{skill.CategoryName}' appeared non-contiguously — results are not grouped by category");
                currentCategory = skill.CategoryName;
            }
        }
    }

    [Test]
    public async Task GetSkillDetail_ReturnsFull_WithLevelsAndPrerequisites()
    {
        var csharpId = (await Db.Skills.FirstAsync(s => s.Name == "C#")).Id;

        var detail = await _service.GetSkillDetailAsync(csharpId);

        Assert.That(detail, Is.Not.Null);
        Assert.That(detail!.Name, Is.EqualTo("C#"));
        Assert.That(detail.LevelCount, Is.EqualTo(7));
        Assert.That(detail.Levels, Has.Count.EqualTo(7));
        Assert.That(detail.Levels.Select(l => l.Niveau).ToList(),
            Is.EqualTo(SevenLevels));
    }

    [Test]
    public async Task GetSkillDetail_ReturnsPrerequisites_ForAspNetCore()
    {
        var aspnetId = (await Db.Skills.FirstAsync(s => s.Name == "ASP.NET Core")).Id;

        var detail = await _service.GetSkillDetailAsync(aspnetId);

        Assert.That(detail, Is.Not.Null);
        Assert.That(detail!.Prerequisites, Is.Not.Empty);
        Assert.That(detail.Prerequisites.Any(p => p.RequiredSkillName == "C#"), Is.True);
    }

    [Test]
    public async Task GetSkillDetail_ReturnsNull_WhenSkillDoesNotExist()
    {
        var detail = await _service.GetSkillDetailAsync(int.MaxValue);

        Assert.That(detail, Is.Null);
    }

    [Test]
    public async Task GetSkillDetail_LevelsAreOrderedByNiveau()
    {
        var javaId = (await Db.Skills.FirstAsync(s => s.Name == "Java")).Id;

        var detail = await _service.GetSkillDetailAsync(javaId);

        var niveaux = detail!.Levels.Select(l => l.Niveau).ToList();
        Assert.That(niveaux, Is.EqualTo(niveaux.Order().ToList()),
            "Levels must be returned in ascending niveau order");
    }

    [Test]
    public async Task GetSkillDetail_CheckboxSkill_HasEmptyLevels()
    {
        var gitId = (await Db.Skills.FirstAsync(s => s.Name == "Git")).Id;

        var detail = await _service.GetSkillDetailAsync(gitId);

        Assert.That(detail!.LevelCount, Is.EqualTo(1));
        Assert.That(detail.Levels, Is.Empty);
    }
}
