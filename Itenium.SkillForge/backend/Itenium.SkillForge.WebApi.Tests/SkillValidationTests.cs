using System.Security.Claims;
using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.Entities.Skills;
using Itenium.SkillForge.Services.Coaching;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class SkillValidationTests : DatabaseTestBase
{
    private ConsultantsController _sut = null!;
    private SkillEntity _skill = null!;
    private ConsultantEntity _consultant = null!;

    [SetUp]
    public async Task SetUp()
    {
        var category = new SkillCategoryEntity { Name = "Test" };
        Db.SkillCategories.Add(category);
        _skill = new SkillEntity { Name = "Clean Code", Category = category, LevelCount = 3 };
        Db.Skills.Add(_skill);
        _consultant = new ConsultantEntity { UserId = "consultant-lea" };
        Db.Consultants.Add(_consultant);
        await Db.SaveChangesAsync();

        _sut = new ConsultantsController(
            new ProfileService(Db),
            new FakeTeamQueryScope(isBackOffice: true),
            new RoadmapService(Db),
            Db,
            new SkillValidationService(Db));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "coach-nathalie"),
                ]))
            }
        };
    }

    [Test]
    public async Task ValidateSkill_WhenConsultantNotFound_ReturnsNotFound()
    {
        var result = await _sut.ValidateSkill(
            int.MaxValue,
            new ValidateSkillRequest(_skill.Id, 2, null));

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task ValidateSkill_WhenConsultantExists_Returns201WithRecord()
    {
        var result = await _sut.ValidateSkill(
            _consultant.Id,
            new ValidateSkillRequest(_skill.Id, 2, null));

        Assert.That(result, Is.TypeOf<CreatedAtActionResult>());
        var created = (CreatedAtActionResult)result;
        var record = (SkillValidationRecord)created.Value!;
        Assert.That(record.Niveau, Is.EqualTo(2));
        Assert.That(record.ConsultantUserId, Is.EqualTo("consultant-lea"));
        Assert.That(record.ValidatedAt, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public async Task ValidateSkill_MultipleTimes_CreatesMultipleRecords()
    {
        await _sut.ValidateSkill(_consultant.Id, new ValidateSkillRequest(_skill.Id, 2, null));
        await _sut.ValidateSkill(_consultant.Id, new ValidateSkillRequest(_skill.Id, 3, null));

        var count = await Db.SkillValidations.CountAsync(v => v.ConsultantUserId == "consultant-lea");
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public async Task ValidateSkill_WithSessionId_LinksToSession()
    {
        var session = new Itenium.SkillForge.Entities.Coaching.CoachingSessionEntity
        {
            ConsultantUserId = "consultant-lea",
            CoachUserId = "coach-nathalie",
        };
        Db.CoachingSessions.Add(session);
        await Db.SaveChangesAsync();

        var result = await _sut.ValidateSkill(
            _consultant.Id,
            new ValidateSkillRequest(_skill.Id, 2, session.Id));

        var created = (CreatedAtActionResult)result;
        var record = (SkillValidationRecord)created.Value!;
        Assert.That(record.SessionId, Is.EqualTo(session.Id));
    }
}
