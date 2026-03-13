using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Profiles;
using Itenium.SkillForge.Services.Import;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Integration tests for <see cref="SkillCatalogueImporter"/> via its EF Core implementation.
/// Tests idempotency and correct entity creation counts.
/// </summary>
[TestFixture]
public class SkillCatalogueImporterTests : DatabaseTestBase
{
    private SkillCatalogueImporter _importer = null!;

    private static ParsedCatalogue MinimalCatalogue => new(
        Skills:
        [
            new("C#", "Language & Runtime", 7, "The C# language."),
            new("Git", "Tooling & DevOps", 1, null),
        ],
        Levels:
        [
            new("C#", 1, "Basic types and control flow"),
            new("C#", 2, "OOP: classes and interfaces"),
        ],
        Prerequisites:
        [
            new("ASP.NET Core", "C#", 2),
        ],
        ProfileSkills:
        [
            new(".NET", "C#"),
            new(".NET", "Git"),
        ],
        SeniorityThresholds:
        [
            new(".NET", SeniorityLevel.Junior, "C#", 2),
            new(".NET", SeniorityLevel.Medior, "C#", 4),
        ]);

    [SetUp]
    public void SetUp()
    {
        _importer = new SkillCatalogueImporter(Db);
    }

    // ── First run ────────────────────────────────────────────────────────────

    [Test]
    public async Task Import_CreatesSkillsOnFirstRun()
    {
        var result = await _importer.ImportAsync(MinimalCatalogue);

        Assert.That(result.SkillsCreated, Is.EqualTo(2));
        Assert.That(await Db.Skills.CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task Import_CreatesCategoriesOnFirstRun()
    {
        var result = await _importer.ImportAsync(MinimalCatalogue);

        Assert.That(result.CategoriesCreated, Is.EqualTo(2));
        Assert.That(await Db.SkillCategories.CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task Import_CreatesLevelsOnFirstRun()
    {
        var result = await _importer.ImportAsync(MinimalCatalogue);

        Assert.That(result.LevelsCreated, Is.EqualTo(2));
        Assert.That(await Db.SkillLevels.CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task Import_SkipsPrerequisites_WhenSkillDoesNotExist()
    {
        // "ASP.NET Core" is not in the catalogue — prerequisite should be skipped
        var result = await _importer.ImportAsync(MinimalCatalogue);

        Assert.That(result.PrerequisitesCreated, Is.EqualTo(0));
        Assert.That(await Db.SkillPrerequisites.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task Import_CreatesPrerequisites_WhenBothSkillsExist()
    {
        var catalogue = MinimalCatalogue with
        {
            Skills = [.. MinimalCatalogue.Skills, new("ASP.NET Core", "Web & API", 5, null)],
        };

        var result = await _importer.ImportAsync(catalogue);

        Assert.That(result.PrerequisitesCreated, Is.EqualTo(1));
        Assert.That(await Db.SkillPrerequisites.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task Import_CreatesProfilesAndProfileSkillsOnFirstRun()
    {
        var result = await _importer.ImportAsync(MinimalCatalogue);

        Assert.That(result.ProfilesCreated, Is.EqualTo(1));
        Assert.That(result.ProfileSkillsCreated, Is.EqualTo(2));
        Assert.That(await Db.CompetenceCentreProfiles.CountAsync(), Is.EqualTo(1));
        Assert.That(await Db.ProfileSkills.CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task Import_CreatesSeniorityThresholdsOnFirstRun()
    {
        var result = await _importer.ImportAsync(MinimalCatalogue);

        Assert.That(result.ThresholdsCreated, Is.EqualTo(2));
        Assert.That(await Db.SeniorityThresholds.CountAsync(), Is.EqualTo(2));
    }

    // ── Idempotency ───────────────────────────────────────────────────────────

    [Test]
    public async Task Import_IsIdempotent_SecondRunCreatesNothing()
    {
        await _importer.ImportAsync(MinimalCatalogue);
        var second = await _importer.ImportAsync(MinimalCatalogue);

        Assert.That(second.SkillsCreated, Is.EqualTo(0));
        Assert.That(second.CategoriesCreated, Is.EqualTo(0));
        Assert.That(second.LevelsCreated, Is.EqualTo(0));
        Assert.That(second.ProfilesCreated, Is.EqualTo(0));
        Assert.That(second.ProfileSkillsCreated, Is.EqualTo(0));
        Assert.That(second.ThresholdsCreated, Is.EqualTo(0));
    }

    [Test]
    public async Task Import_IsIdempotent_SecondRunDoesNotDuplicate()
    {
        await _importer.ImportAsync(MinimalCatalogue);
        await _importer.ImportAsync(MinimalCatalogue);

        Assert.That(await Db.Skills.CountAsync(), Is.EqualTo(2));
        Assert.That(await Db.SkillCategories.CountAsync(), Is.EqualTo(2));
    }

    // ── Data integrity ────────────────────────────────────────────────────────

    [Test]
    public async Task Import_SetsSkillFieldsCorrectly()
    {
        await _importer.ImportAsync(MinimalCatalogue);

        var csharp = await Db.Skills
            .Include(s => s.Category)
            .FirstAsync(s => s.Name == "C#");

        Assert.That(csharp.LevelCount, Is.EqualTo(7));
        Assert.That(csharp.Description, Is.EqualTo("The C# language."));
        Assert.That(csharp.Category.Name, Is.EqualTo("Language & Runtime"));
    }

    [Test]
    public async Task Import_SetsCheckboxSkillWithLevelCountOne()
    {
        await _importer.ImportAsync(MinimalCatalogue);

        var git = await Db.Skills.FirstAsync(s => s.Name == "Git");
        Assert.That(git.LevelCount, Is.EqualTo(1));
    }
}
