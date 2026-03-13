using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.Entities.Skills;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class SkillValidationControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private SkillValidationController _sut = null!;
    private SkillCategoryEntity _category = null!;
    private SkillEntity _skill = null!;

    [SetUp]
    public async Task Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        _sut = new SkillValidationController(Db, scope, _user);

        _category = new SkillCategoryEntity { Name = "Test Category" };
        Db.SkillCategories.Add(_category);
        _skill = new SkillEntity { Name = "Clean Code", Category = _category, LevelCount = 3 };
        Db.Skills.Add(_skill);
        await Db.SaveChangesAsync();
    }

    [Test]
    public async Task ValidateSkill_WhenManager_CreatesImmutableRecord()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        _sut = new SkillValidationController(Db, scope, _user);
        _user.UserId.Returns("coach-1");

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-1", TeamId = 1 });
        await Db.SaveChangesAsync();

        var request = new ValidateSkillRequest("consultant-1", _skill.Id, 2, null);
        var result = await _sut.ValidateSkill(request);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var validation = created!.Value as SkillValidationEntity;
        Assert.That(validation!.ConsultantUserId, Is.EqualTo("consultant-1"));
        Assert.That(validation.CoachUserId, Is.EqualTo("coach-1"));
        Assert.That(validation.Niveau, Is.EqualTo(2));
        Assert.That(validation.ValidatedAt, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public async Task ValidateSkill_MultipleValidations_CreatesMultipleRecords()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        _sut = new SkillValidationController(Db, scope, _user);
        _user.UserId.Returns("coach-1");

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-1", TeamId = 1 });
        await Db.SaveChangesAsync();

        await _sut.ValidateSkill(new ValidateSkillRequest("consultant-1", _skill.Id, 1, null));
        await _sut.ValidateSkill(new ValidateSkillRequest("consultant-1", _skill.Id, 2, null));

        var count = Db.SkillValidations.Count(v => v.ConsultantUserId == "consultant-1" && v.SkillId == _skill.Id);
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public async Task ValidateSkill_WhenLearner_ReturnsForbidden()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: []);
        _sut = new SkillValidationController(Db, scope, _user);

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-1", TeamId = 1 });
        await Db.SaveChangesAsync();

        var request = new ValidateSkillRequest("consultant-1", _skill.Id, 2, null);
        var result = await _sut.ValidateSkill(request);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task ValidateSkill_WhenManagerForDifferentTeam_ReturnsForbidden()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [2]);
        _sut = new SkillValidationController(Db, scope, _user);

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-team-1", TeamId = 1 });
        await Db.SaveChangesAsync();

        var request = new ValidateSkillRequest("consultant-team-1", _skill.Id, 2, null);
        var result = await _sut.ValidateSkill(request);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task GetValidations_WhenManager_ReturnsConsultantValidations()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        _sut = new SkillValidationController(Db, scope, _user);

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-1", TeamId = 1 });
        Db.SkillValidations.Add(new SkillValidationEntity
        {
            ConsultantUserId = "consultant-1",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            Niveau = 2,
        });
        await Db.SaveChangesAsync();

        var result = await _sut.GetValidations("consultant-1");

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var validations = ok!.Value as List<SkillValidationEntity>;
        Assert.That(validations, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetValidations_WhenLearner_ReturnsOwnValidations()
    {
        _user.UserId.Returns("learner-1");
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: []);
        _sut = new SkillValidationController(Db, scope, _user);

        Db.SkillValidations.Add(new SkillValidationEntity
        {
            ConsultantUserId = "learner-1",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            Niveau = 1,
        });
        Db.SkillValidations.Add(new SkillValidationEntity
        {
            ConsultantUserId = "other-consultant",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            Niveau = 2,
        });
        await Db.SaveChangesAsync();

        var result = await _sut.GetValidations("learner-1");

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var validations = ok!.Value as List<SkillValidationEntity>;
        Assert.That(validations, Has.Count.EqualTo(1));
        Assert.That(validations![0].ConsultantUserId, Is.EqualTo("learner-1"));
    }
}
