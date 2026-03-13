using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class ProfilesControllerTests : DatabaseTestBase
{
    private ProfilesController _sut = null!;

    [SetUp]
    public async Task SetUp()
    {
        await SkillCatalogueSeedData.Seed(Db);
        _sut = new ProfilesController(new ProfileService(Db));
    }

    // ── GET /api/profiles ────────────────────────────────────────────────────

    [Test]
    public async Task GetProfiles_ReturnsOkWithList()
    {
        var result = await _sut.GetProfiles();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.Not.Null);
    }

    // ── GET /api/profiles/{id}/skills ────────────────────────────────────────

    [Test]
    public async Task GetProfileSkills_WhenProfileNotFound_ReturnsNotFound()
    {
        var result = await _sut.GetProfileSkills(int.MaxValue);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task GetProfileSkills_ForDotNetProfile_ReturnsOkWithSkills()
    {
        var dotnetId = (await Db.CompetenceCentreProfiles
            .FirstAsync(p => p.Name == ".NET")).Id;

        var result = await _sut.GetProfileSkills(dotnetId);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.Not.Null);
    }

    // ── GET /api/profiles/{id}/seniority-thresholds ──────────────────────────

    [Test]
    public async Task GetSeniorityThresholds_WhenProfileNotFound_ReturnsNotFound()
    {
        var result = await _sut.GetSeniorityThresholds(int.MaxValue);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task GetSeniorityThresholds_ForDotNetProfile_ReturnsOkWithGroupedThresholds()
    {
        var dotnetId = (await Db.CompetenceCentreProfiles
            .FirstAsync(p => p.Name == ".NET")).Id;

        var result = await _sut.GetSeniorityThresholds(dotnetId);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.Not.Null);
    }
}
