using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class ConsultantsControllerTests : DatabaseTestBase
{
    private ConsultantsController _sut = null!;

    [SetUp]
    public async Task SetUp()
    {
        await SkillCatalogueSeedData.Seed(Db);
        _sut = new ConsultantsController(new ProfileService(Db), new FakeTeamQueryScope(isBackOffice: true), new RoadmapService(Db), Db, new SkillValidationService(Db));
    }

    [Test]
    public async Task AssignProfile_WhenConsultantNotFound_ReturnsNotFound()
    {
        var dotnetId = (await Db.CompetenceCentreProfiles
            .FirstAsync(p => p.Name == ".NET")).Id;

        var result = await _sut.AssignProfile(int.MaxValue, new AssignProfileRequest(dotnetId));

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task AssignProfile_WhenConsultantExists_ReturnsNoContent()
    {
        var consultant = new ConsultantEntity { UserId = "user-ctrl-1" };
        Db.Consultants.Add(consultant);
        await Db.SaveChangesAsync();
        var dotnetId = (await Db.CompetenceCentreProfiles
            .FirstAsync(p => p.Name == ".NET")).Id;

        var result = await _sut.AssignProfile(consultant.Id, new AssignProfileRequest(dotnetId));

        Assert.That(result, Is.TypeOf<NoContentResult>());
    }

    [Test]
    public async Task AssignProfile_WithNullProfileId_ClearsProfile_ReturnsNoContent()
    {
        var dotnetId = (await Db.CompetenceCentreProfiles
            .FirstAsync(p => p.Name == ".NET")).Id;
        var consultant = new ConsultantEntity { UserId = "user-ctrl-2", ProfileId = dotnetId };
        Db.Consultants.Add(consultant);
        await Db.SaveChangesAsync();

        var result = await _sut.AssignProfile(consultant.Id, new AssignProfileRequest(null));

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var updated = await Db.Consultants.FindAsync(consultant.Id);
        Assert.That(updated!.ProfileId, Is.Null);
    }
}
