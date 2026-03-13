using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.Entities.Profiles;
using Itenium.SkillForge.Services.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Integration tests for <see cref="ProfileService"/> via its EF Core implementation.
/// Seed data is applied before each test and rolled back on teardown.
/// </summary>
[TestFixture]
public class ProfileServiceTests : DatabaseTestBase
{
    private static readonly string[] DotNetProfiles = [".NET", "Java"];
    private static readonly string[] DotNetCoreSkills = ["C#", "ASP.NET Core", "Entity Framework Core"];

    private ProfileService _service = null!;

    [SetUp]
    public async Task SetUp()
    {
        await SkillCatalogueSeedData.Seed(Db);
        _service = new ProfileService(Db);
    }

    // ── GetProfilesAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task GetProfiles_ReturnsAllProfiles()
    {
        var profiles = await _service.GetProfilesAsync();

        Assert.That(profiles, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetProfiles_ContainsDotNetAndJava()
    {
        var profiles = await _service.GetProfilesAsync();

        var names = profiles.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.That(names, Is.SupersetOf(DotNetProfiles));
    }

    // ── GetProfileSkillsAsync ────────────────────────────────────────────────

    [Test]
    public async Task GetProfileSkills_ReturnsNull_WhenProfileDoesNotExist()
    {
        var skills = await _service.GetProfileSkillsAsync(int.MaxValue);

        Assert.That(skills, Is.Null);
    }

    [Test]
    public async Task GetProfileSkills_ReturnsDotNetSkills_ForDotNetProfile()
    {
        var dotnet = await Db.CompetenceCentreProfiles.FirstAsync(p => p.Name == ".NET");

        var skills = await _service.GetProfileSkillsAsync(dotnet.Id);

        Assert.That(skills, Is.Not.Null);
        var names = skills!.Select(s => s.Name).ToHashSet(StringComparer.Ordinal);
        Assert.That(names, Is.SupersetOf(DotNetCoreSkills));
        Assert.That(names.Contains("Spring Boot", StringComparer.Ordinal), Is.False,
            "Java-specific skills must not appear in .NET profile");
    }

    [Test]
    public async Task GetProfileSkills_AreGroupedByCategory()
    {
        var profile = await Db.CompetenceCentreProfiles.FirstAsync(p => p.Name == ".NET");

        var skills = await _service.GetProfileSkillsAsync(profile.Id);

        Assert.That(skills, Is.Not.Null);

        // Skills with the same category must appear in a contiguous block.
        var seenCategories = new HashSet<string>(StringComparer.Ordinal);
        string? currentCategory = null;
        foreach (var skill in skills!)
        {
            if (!string.Equals(skill.CategoryName, currentCategory, StringComparison.Ordinal))
            {
                Assert.That(seenCategories.Add(skill.CategoryName), Is.True,
                    $"Category '{skill.CategoryName}' appeared non-contiguously — results are not grouped by category");
                currentCategory = skill.CategoryName;
            }
        }
    }

    // ── GetSeniorityThresholdsAsync ──────────────────────────────────────────

    [Test]
    public async Task GetSeniorityThresholds_ReturnsNull_WhenProfileDoesNotExist()
    {
        var result = await _service.GetSeniorityThresholdsAsync(int.MaxValue);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetSeniorityThresholds_ReturnsThreeGroups_ForDotNetProfile()
    {
        var dotnet = await Db.CompetenceCentreProfiles.FirstAsync(p => p.Name == ".NET");

        var result = await _service.GetSeniorityThresholdsAsync(dotnet.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(3), "Expected Junior, Medior, Senior groups");
        var levels = result.Select(r => r.Level).ToHashSet();
        Assert.That(levels, Is.EquivalentTo(Enum.GetValues<SeniorityLevel>()));
    }

    [Test]
    public async Task GetSeniorityThresholds_EachGroupHasThresholds()
    {
        var dotnet = await Db.CompetenceCentreProfiles.FirstAsync(p => p.Name == ".NET");

        var result = await _service.GetSeniorityThresholdsAsync(dotnet.Id);

        Assert.That(result, Is.Not.Null);
        foreach (var group in result!)
        {
            Assert.That(group.Thresholds, Is.Not.Empty,
                $"Level {group.Level} must have at least one threshold");
        }
    }

    [Test]
    public async Task GetSeniorityThresholds_ThresholdContainsSkillName()
    {
        var dotnet = await Db.CompetenceCentreProfiles.FirstAsync(p => p.Name == ".NET");

        var result = await _service.GetSeniorityThresholdsAsync(dotnet.Id);

        var allSkillNames = result!
            .SelectMany(g => g.Thresholds)
            .Select(t => t.SkillName)
            .ToHashSet(StringComparer.Ordinal);

        Assert.That(allSkillNames.Contains("C#", StringComparer.Ordinal), Is.True,
            "C# should appear in .NET seniority thresholds");
    }

    // ── AssignProfileToConsultantAsync ───────────────────────────────────────

    [Test]
    public async Task AssignProfile_ReturnsFalse_WhenConsultantDoesNotExist()
    {
        var dotnet = await Db.CompetenceCentreProfiles.FirstAsync(p => p.Name == ".NET");

        var result = await _service.AssignProfileToConsultantAsync(int.MaxValue, dotnet.Id, new FakeTeamQueryScope(isBackOffice: true));

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task AssignProfile_SetsProfile_WhenConsultantExists()
    {
        var consultant = new ConsultantEntity { UserId = "user-assign-1" };
        Db.Consultants.Add(consultant);
        await Db.SaveChangesAsync();
        var dotnet = await Db.CompetenceCentreProfiles.FirstAsync(p => p.Name == ".NET");

        var result = await _service.AssignProfileToConsultantAsync(consultant.Id, dotnet.Id, new FakeTeamQueryScope(isBackOffice: true));

        Assert.That(result, Is.True);
        var updated = await Db.Consultants.FindAsync(consultant.Id);
        Assert.That(updated!.ProfileId, Is.EqualTo(dotnet.Id));
    }

    [Test]
    public async Task AssignProfile_ClearsProfile_WhenProfileIdIsNull()
    {
        var dotnet = await Db.CompetenceCentreProfiles.FirstAsync(p => p.Name == ".NET");
        var consultant = new ConsultantEntity { UserId = "user-assign-2", ProfileId = dotnet.Id };
        Db.Consultants.Add(consultant);
        await Db.SaveChangesAsync();

        var result = await _service.AssignProfileToConsultantAsync(consultant.Id, null, new FakeTeamQueryScope(isBackOffice: true));

        Assert.That(result, Is.True);
        var updated = await Db.Consultants.FindAsync(consultant.Id);
        Assert.That(updated!.ProfileId, Is.Null);
    }
}
