using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.Entities.Goals;
using Itenium.SkillForge.Entities.Skills;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class CoachDashboardControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private SkillCategoryEntity _category = null!;
    private SkillEntity _skill = null!;

    [SetUp]
    public async Task Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();

        _category = new SkillCategoryEntity { Name = "Test Category" };
        Db.SkillCategories.Add(_category);
        _skill = new SkillEntity { Name = "Clean Code", Category = _category, LevelCount = 3 };
        Db.Skills.Add(_skill);
        await Db.SaveChangesAsync();
    }

    [Test]
    public async Task GetDashboard_WhenManager_ReturnsTeamConsultants()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        var sut = new CoachDashboardController(Db, scope);

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-team-1", TeamId = 1 });
        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-team-2", TeamId = 2 });
        await Db.SaveChangesAsync();

        var result = await sut.GetDashboard();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var rows = ok!.Value as List<ConsultantDashboardRow>;
        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows![0].UserId, Is.EqualTo("consultant-team-1"));
    }

    [Test]
    public async Task GetDashboard_ShowsReadinessFlagCount()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        var sut = new CoachDashboardController(Db, scope);

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
        await Db.SaveChangesAsync();

        Db.ReadinessFlags.Add(new ReadinessFlagEntity { GoalId = goal.Id });
        await Db.SaveChangesAsync();

        var result = await sut.GetDashboard();

        var ok = result.Result as OkObjectResult;
        var rows = ok!.Value as List<ConsultantDashboardRow>;
        Assert.That(rows![0].ReadinessFlagCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GetDashboard_ShowsOverdueGoalCount()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        var sut = new CoachDashboardController(Db, scope);

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-1", TeamId = 1 });
        Db.Goals.Add(new GoalEntity
        {
            ConsultantUserId = "consultant-1",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 0,
            TargetNiveau = 2,
            Deadline = DateTime.UtcNow.AddDays(-5),
            Status = GoalStatus.Active,
        });
        await Db.SaveChangesAsync();

        var result = await sut.GetDashboard();

        var ok = result.Result as OkObjectResult;
        var rows = ok!.Value as List<ConsultantDashboardRow>;
        Assert.That(rows![0].OverdueGoalCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GetDashboard_ShowsInactiveConsultants()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        var sut = new CoachDashboardController(Db, scope);

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-inactive", TeamId = 1 });
        Db.CoachingSessions.Add(new CoachingSessionEntity
        {
            ConsultantUserId = "consultant-inactive",
            CoachUserId = "coach-1",
            ClosedAt = DateTime.UtcNow.AddDays(-25),
        });
        await Db.SaveChangesAsync();

        var result = await sut.GetDashboard();

        var ok = result.Result as OkObjectResult;
        var rows = ok!.Value as List<ConsultantDashboardRow>;
        Assert.That(rows![0].IsInactive, Is.True);
    }

    [Test]
    public async Task GetDashboard_WhenBackOffice_ReturnsAllConsultants()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: true);
        var sut = new CoachDashboardController(Db, scope);

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-team-1", TeamId = 1 });
        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-team-2", TeamId = 2 });
        await Db.SaveChangesAsync();

        var result = await sut.GetDashboard();

        var ok = result.Result as OkObjectResult;
        var rows = ok!.Value as List<ConsultantDashboardRow>;
        Assert.That(rows, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetConsultantActivity_ReturnsGoalsAndSessions()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        var sut = new CoachDashboardController(Db, scope);

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-1", TeamId = 1 });
        Db.Goals.Add(new GoalEntity
        {
            ConsultantUserId = "consultant-1",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 0,
            TargetNiveau = 2,
        });
        Db.CoachingSessions.Add(new CoachingSessionEntity
        {
            ConsultantUserId = "consultant-1",
            CoachUserId = "coach-1",
        });
        await Db.SaveChangesAsync();

        var result = await sut.GetConsultantActivity("consultant-1");

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var activity = ok!.Value as ConsultantActivityResponse;
        Assert.That(activity!.Goals, Has.Count.EqualTo(1));
        Assert.That(activity.Sessions, Has.Count.EqualTo(1));
    }
}
