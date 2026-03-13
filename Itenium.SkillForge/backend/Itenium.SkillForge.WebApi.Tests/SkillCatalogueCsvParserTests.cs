using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Profiles;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Unit tests for <see cref="SkillCatalogueCsvParser"/>.
/// No database required — purely tests parsing logic.
/// </summary>
[TestFixture]
public class SkillCatalogueCsvParserTests
{
    private static readonly string SkillsCsv = """
        Name,Category,LevelCount,Description
        C#,Language & Runtime,7,"The C# language, from fundamentals to advanced."
        Git,Tooling & DevOps,1,
        """;

    private static readonly string LevelsCsv = """
        SkillName,Niveau,Descriptor
        C#,1,Writes simple procedural code
        C#,2,Uses OOP: classes and interfaces
        """;

    private static readonly string PrerequisitesCsv = """
        SkillName,RequiredSkillName,RequiredMinNiveau
        ASP.NET Core,C#,2
        """;

    private static readonly string ProfileSkillsCsv = """
        ProfileName,SkillName
        .NET,C#
        .NET,Git
        Java,Git
        """;

    private static readonly string SeniorityThresholdsCsv = """
        ProfileName,SeniorityLevel,SkillName,MinNiveau
        .NET,Junior,C#,2
        .NET,Medior,C#,4
        .NET,Senior,C#,6
        """;

    // ── Skills ───────────────────────────────────────────────────────────────

    [Test]
    public void ParseSkills_ReturnsCorrectCount()
    {
        var result = SkillCatalogueCsvParser.ParseSkills(SkillsCsv);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void ParseSkills_MapsAllFields()
    {
        var result = SkillCatalogueCsvParser.ParseSkills(SkillsCsv);

        var csharp = result.Single(s => s.Name == "C#");
        Assert.That(csharp.Category, Is.EqualTo("Language & Runtime"));
        Assert.That(csharp.LevelCount, Is.EqualTo(7));
        Assert.That(csharp.Description, Is.EqualTo("The C# language, from fundamentals to advanced."));
    }

    [Test]
    public void ParseSkills_HandlesEmptyDescription()
    {
        var result = SkillCatalogueCsvParser.ParseSkills(SkillsCsv);

        var git = result.Single(s => s.Name == "Git");
        Assert.That(git.Description, Is.Null.Or.Empty);
    }

    // ── Levels ───────────────────────────────────────────────────────────────

    [Test]
    public void ParseLevels_ReturnsCorrectCount()
    {
        var result = SkillCatalogueCsvParser.ParseLevels(LevelsCsv);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void ParseLevels_MapsAllFields()
    {
        var result = SkillCatalogueCsvParser.ParseLevels(LevelsCsv);

        var level1 = result.Single(l => l.Niveau == 1);
        Assert.That(level1.SkillName, Is.EqualTo("C#"));
        Assert.That(level1.Descriptor, Is.EqualTo("Writes simple procedural code"));
    }

    // ── Prerequisites ────────────────────────────────────────────────────────

    [Test]
    public void ParsePrerequisites_ReturnsCorrectCount()
    {
        var result = SkillCatalogueCsvParser.ParsePrerequisites(PrerequisitesCsv);

        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public void ParsePrerequisites_MapsAllFields()
    {
        var result = SkillCatalogueCsvParser.ParsePrerequisites(PrerequisitesCsv);

        var prereq = result.Single();
        Assert.That(prereq.SkillName, Is.EqualTo("ASP.NET Core"));
        Assert.That(prereq.RequiredSkillName, Is.EqualTo("C#"));
        Assert.That(prereq.RequiredMinNiveau, Is.EqualTo(2));
    }

    // ── Profile skills ───────────────────────────────────────────────────────

    [Test]
    public void ParseProfileSkills_ReturnsCorrectCount()
    {
        var result = SkillCatalogueCsvParser.ParseProfileSkills(ProfileSkillsCsv);

        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public void ParseProfileSkills_MapsAllFields()
    {
        var result = SkillCatalogueCsvParser.ParseProfileSkills(ProfileSkillsCsv);

        Assert.That(result.Any(r => r.ProfileName == ".NET" && r.SkillName == "C#"), Is.True);
    }

    // ── Seniority thresholds ─────────────────────────────────────────────────

    [Test]
    public void ParseSeniorityThresholds_ReturnsCorrectCount()
    {
        var result = SkillCatalogueCsvParser.ParseSeniorityThresholds(SeniorityThresholdsCsv);

        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public void ParseSeniorityThresholds_MapsAllFields()
    {
        var result = SkillCatalogueCsvParser.ParseSeniorityThresholds(SeniorityThresholdsCsv);

        var junior = result.Single(r => r.SeniorityLevel == SeniorityLevel.Junior);
        Assert.That(junior.ProfileName, Is.EqualTo(".NET"));
        Assert.That(junior.SkillName, Is.EqualTo("C#"));
        Assert.That(junior.MinNiveau, Is.EqualTo(2));
    }

    [Test]
    public void ParseSeniorityThresholds_ParsesAllSeniorityLevels()
    {
        var result = SkillCatalogueCsvParser.ParseSeniorityThresholds(SeniorityThresholdsCsv);

        var levels = result.Select(r => r.SeniorityLevel).ToHashSet();
        Assert.That(levels, Is.EquivalentTo(Enum.GetValues<SeniorityLevel>()));
    }

    // ── ParseAll ─────────────────────────────────────────────────────────────

    [Test]
    public void ParseAll_ReturnsCombinedCatalogue()
    {
        var catalogue = SkillCatalogueCsvParser.ParseAll(
            SkillsCsv, LevelsCsv, PrerequisitesCsv, ProfileSkillsCsv, SeniorityThresholdsCsv);

        Assert.That(catalogue.Skills, Has.Count.EqualTo(2));
        Assert.That(catalogue.Levels, Has.Count.EqualTo(2));
        Assert.That(catalogue.Prerequisites, Has.Count.EqualTo(1));
        Assert.That(catalogue.ProfileSkills, Has.Count.EqualTo(3));
        Assert.That(catalogue.SeniorityThresholds, Has.Count.EqualTo(3));
    }
}
