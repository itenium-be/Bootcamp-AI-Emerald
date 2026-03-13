using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.Entities.Goals;
using Itenium.SkillForge.Entities.Resources;
using Itenium.SkillForge.Entities.Skills;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class GoalControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private GoalController _sut = null!;
    private SkillCategoryEntity _category = null!;
    private SkillEntity _skill = null!;

    [SetUp]
    public async Task Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        _sut = new GoalController(Db, scope, _user);

        _category = new SkillCategoryEntity { Name = "Test Category" };
        Db.SkillCategories.Add(_category);
        _skill = new SkillEntity { Name = "Clean Code", Category = _category, LevelCount = 3 };
        Db.Skills.Add(_skill);
        await Db.SaveChangesAsync();
    }

    [Test]
    public async Task GetMyGoals_WhenLearner_ReturnsOwnActiveGoals()
    {
        _user.UserId.Returns("learner-1");
        var goal = new GoalEntity
        {
            ConsultantUserId = "learner-1",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 1,
            TargetNiveau = 3,
            Status = GoalStatus.Active,
        };
        Db.Goals.Add(goal);
        // Goal for another user — should not appear
        Db.Goals.Add(new GoalEntity
        {
            ConsultantUserId = "other-user",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 1,
            TargetNiveau = 2,
        });
        await Db.SaveChangesAsync();

        var result = await _sut.GetMyGoals();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var goals = ok!.Value as List<GoalEntity>;
        Assert.That(goals, Has.Count.EqualTo(1));
        Assert.That(goals![0].ConsultantUserId, Is.EqualTo("learner-1"));
    }

    [Test]
    public async Task GetMyGoals_WhenNoGoals_ReturnsEmpty()
    {
        _user.UserId.Returns("learner-no-goals");

        var result = await _sut.GetMyGoals();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var goals = ok!.Value as List<GoalEntity>;
        Assert.That(goals, Is.Empty);
    }

    [Test]
    public async Task GetConsultantGoals_WhenManagerForTeam_ReturnsGoals()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        _sut = new GoalController(Db, scope, _user);

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-in-team", TeamId = 1 });
        await Db.SaveChangesAsync();

        Db.Goals.Add(new GoalEntity
        {
            ConsultantUserId = "consultant-in-team",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 1,
            TargetNiveau = 2,
        });
        await Db.SaveChangesAsync();

        var result = await _sut.GetConsultantGoals("consultant-in-team");

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var goals = ok!.Value as List<GoalEntity>;
        Assert.That(goals, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetConsultantGoals_WhenManagerForDifferentTeam_ReturnsForbidden()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [2]);
        _sut = new GoalController(Db, scope, _user);

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-team-1", TeamId = 1 });
        await Db.SaveChangesAsync();

        var result = await _sut.GetConsultantGoals("consultant-team-1");

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task CreateGoal_WhenManager_CreatesGoal()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        _sut = new GoalController(Db, scope, _user);
        _user.UserId.Returns("manager-1");

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-1", TeamId = 1 });
        await Db.SaveChangesAsync();

        var request = new CreateGoalRequest("consultant-1", _skill.Id, 0, 2, null);
        var result = await _sut.CreateGoal(request);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var goal = created!.Value as GoalEntity;
        Assert.That(goal!.ConsultantUserId, Is.EqualTo("consultant-1"));
        Assert.That(goal.CoachUserId, Is.EqualTo("manager-1"));
    }

    [Test]
    public async Task CreateGoal_WhenLearner_ReturnsForbidden()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: []);
        _sut = new GoalController(Db, scope, _user);

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-1", TeamId = 1 });
        await Db.SaveChangesAsync();

        var request = new CreateGoalRequest("consultant-1", _skill.Id, 0, 2, null);
        var result = await _sut.CreateGoal(request);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task UpdateGoal_WhenManager_UpdatesGoal()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        _sut = new GoalController(Db, scope, _user);

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-1", TeamId = 1 });
        var goal = new GoalEntity
        {
            ConsultantUserId = "consultant-1",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 0,
            TargetNiveau = 1,
        };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();

        var request = new UpdateGoalRequest(3, DateTime.UtcNow.AddMonths(1), GoalStatus.Achieved);
        var result = await _sut.UpdateGoal(goal.Id, request);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var updated = ok!.Value as GoalEntity;
        Assert.That(updated!.TargetNiveau, Is.EqualTo(3));
        Assert.That(updated.Status, Is.EqualTo(GoalStatus.Achieved));
    }

    [Test]
    public async Task UpdateGoal_WhenGoalNotFound_ReturnsNotFound()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        _sut = new GoalController(Db, scope, _user);

        var request = new UpdateGoalRequest(3, null, null);
        var result = await _sut.UpdateGoal(999, request);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task SignalReadiness_WhenLearnerAndNoExistingFlag_CreatesFlag()
    {
        _user.UserId.Returns("learner-1");

        var goal = new GoalEntity
        {
            ConsultantUserId = "learner-1",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 1,
            TargetNiveau = 3,
        };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();

        var result = await _sut.SignalReadiness(goal.Id);

        Assert.That(result, Is.TypeOf<CreatedResult>());
        var flag = await Db.ReadinessFlags.FindAsync(
            Db.ReadinessFlags.Where(f => f.GoalId == goal.Id).Select(f => f.Id).FirstOrDefault());
        Assert.That(flag, Is.Not.Null);
    }

    [Test]
    public async Task SignalReadiness_WhenFlagAlreadyExists_ReturnsBadRequest()
    {
        _user.UserId.Returns("learner-1");

        var goal = new GoalEntity
        {
            ConsultantUserId = "learner-1",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 1,
            TargetNiveau = 3,
        };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();

        Db.ReadinessFlags.Add(new ReadinessFlagEntity { GoalId = goal.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.SignalReadiness(goal.Id);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task DismissReadiness_WhenManager_DismissesFlag()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        _sut = new GoalController(Db, scope, _user);

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-1", TeamId = 1 });
        var goal = new GoalEntity
        {
            ConsultantUserId = "consultant-1",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 1,
            TargetNiveau = 3,
        };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();

        var flag = new ReadinessFlagEntity { GoalId = goal.Id };
        Db.ReadinessFlags.Add(flag);
        await Db.SaveChangesAsync();

        var result = await _sut.DismissReadiness(goal.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(flag.DismissedAt, Is.Not.Null);
    }

    [Test]
    public async Task AddResourceToGoal_WhenManager_AddsLink()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        _sut = new GoalController(Db, scope, _user);

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-1", TeamId = 1 });
        var goal = new GoalEntity
        {
            ConsultantUserId = "consultant-1",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 0,
            TargetNiveau = 2,
        };
        Db.Goals.Add(goal);
        var resource = new ResourceEntity
        {
            Title = "Clean Code Book",
            Url = "https://example.com",
            Type = ResourceType.Book,
            SkillId = _skill.Id,
            FromNiveau = 0,
            ToNiveau = 3,
            AddedByUserId = "coach-1",
        };
        Db.Resources.Add(resource);
        await Db.SaveChangesAsync();

        var result = await _sut.AddResourceToGoal(goal.Id, resource.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(Db.GoalResources.Any(gr => gr.GoalId == goal.Id && gr.ResourceId == resource.Id), Is.True);
    }

    [Test]
    public async Task RemoveResourceFromGoal_WhenManager_RemovesLink()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        _sut = new GoalController(Db, scope, _user);

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-1", TeamId = 1 });
        var goal = new GoalEntity
        {
            ConsultantUserId = "consultant-1",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 0,
            TargetNiveau = 2,
        };
        Db.Goals.Add(goal);
        var resource = new ResourceEntity
        {
            Title = "Clean Code Book",
            Url = "https://example.com",
            Type = ResourceType.Book,
            SkillId = _skill.Id,
            FromNiveau = 0,
            ToNiveau = 3,
            AddedByUserId = "coach-1",
        };
        Db.Resources.Add(resource);
        await Db.SaveChangesAsync();

        Db.GoalResources.Add(new GoalResourceEntity { GoalId = goal.Id, ResourceId = resource.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.RemoveResourceFromGoal(goal.Id, resource.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(Db.GoalResources.Any(gr => gr.GoalId == goal.Id && gr.ResourceId == resource.Id), Is.False);
    }

    [Test]
    public async Task GetOverdueGoals_ReturnsOnlyOverdueActiveGoals()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        _sut = new GoalController(Db, scope, _user);

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-1", TeamId = 1 });
        await Db.SaveChangesAsync();

        // Overdue active goal
        Db.Goals.Add(new GoalEntity
        {
            ConsultantUserId = "consultant-1",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 0,
            TargetNiveau = 2,
            Deadline = DateTime.UtcNow.AddDays(-1),
            Status = GoalStatus.Active,
        });
        // Not overdue
        Db.Goals.Add(new GoalEntity
        {
            ConsultantUserId = "consultant-1",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 0,
            TargetNiveau = 2,
            Deadline = DateTime.UtcNow.AddDays(10),
            Status = GoalStatus.Active,
        });
        // Overdue but achieved — should not appear
        Db.Goals.Add(new GoalEntity
        {
            ConsultantUserId = "consultant-1",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 0,
            TargetNiveau = 2,
            Deadline = DateTime.UtcNow.AddDays(-5),
            Status = GoalStatus.Achieved,
        });
        await Db.SaveChangesAsync();

        var result = await _sut.GetOverdueGoals();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var goals = ok!.Value as List<GoalEntity>;
        Assert.That(goals, Has.Count.EqualTo(1));
    }
}
