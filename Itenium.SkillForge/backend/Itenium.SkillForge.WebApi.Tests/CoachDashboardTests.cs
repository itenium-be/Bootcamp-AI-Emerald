using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.Entities.Goals;
using Itenium.SkillForge.Entities.Resources;
using Itenium.SkillForge.Entities.Skills;
using Itenium.SkillForge.Services.Coaching;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class CoachDashboardTests : DatabaseTestBase
{
    private CoachDashboardController _sut = null!;
    private ConsultantEntity _consultant = null!;
    private SkillEntity _skill = null!;

    [SetUp]
    public async Task SetUp()
    {
        var category = new SkillCategoryEntity { Name = "Test" };
        Db.SkillCategories.Add(category);
        _skill = new SkillEntity { Name = "Clean Code", Category = category, LevelCount = 3 };
        Db.Skills.Add(_skill);
        _consultant = new ConsultantEntity { UserId = "consultant-lea", TeamId = 1 };
        Db.Consultants.Add(_consultant);
        await Db.SaveChangesAsync();

        _sut = new CoachDashboardController(
            new CoachDashboardService(Db),
            new FakeTeamQueryScope(isBackOffice: true));
    }

    // ── Dashboard row ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetDashboard_ReturnsConsultantRow()
    {
        var result = await _sut.GetDashboard();

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result;
        var rows = (IReadOnlyList<CoachDashboardRow>)ok.Value!;
        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].UserId, Is.EqualTo("consultant-lea"));
    }

    [Test]
    public async Task GetDashboard_CountsActiveGoals()
    {
        Db.Goals.Add(new GoalEntity { ConsultantUserId = "consultant-lea", CoachUserId = "coach", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 2, Status = GoalStatus.Active });
        Db.Goals.Add(new GoalEntity { ConsultantUserId = "consultant-lea", CoachUserId = "coach", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 2, Status = GoalStatus.Achieved });
        await Db.SaveChangesAsync();

        var result = await _sut.GetDashboard();
        var rows = (IReadOnlyList<CoachDashboardRow>)((OkObjectResult)result).Value!;

        Assert.That(rows[0].ActiveGoalCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GetDashboard_CountsOverdueGoals()
    {
        Db.Goals.Add(new GoalEntity { ConsultantUserId = "consultant-lea", CoachUserId = "coach", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 2, Status = GoalStatus.Active, Deadline = DateTime.UtcNow.AddDays(-1) });
        Db.Goals.Add(new GoalEntity { ConsultantUserId = "consultant-lea", CoachUserId = "coach", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 2, Status = GoalStatus.Active, Deadline = DateTime.UtcNow.AddDays(10) });
        await Db.SaveChangesAsync();

        var result = await _sut.GetDashboard();
        var rows = (IReadOnlyList<CoachDashboardRow>)((OkObjectResult)result).Value!;

        Assert.That(rows[0].OverdueGoalCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GetDashboard_CountsActiveReadinessFlags()
    {
        var activeGoal = new GoalEntity { ConsultantUserId = "consultant-lea", CoachUserId = "coach", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 2 };
        var dismissedGoal = new GoalEntity { ConsultantUserId = "consultant-lea", CoachUserId = "coach", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 3 };
        Db.Goals.AddRange(activeGoal, dismissedGoal);
        await Db.SaveChangesAsync();

        Db.ReadinessFlags.Add(new ReadinessFlagEntity { GoalId = activeGoal.Id });
        Db.ReadinessFlags.Add(new ReadinessFlagEntity { GoalId = dismissedGoal.Id, DismissedAt = DateTime.UtcNow });
        await Db.SaveChangesAsync();

        var result = await _sut.GetDashboard();
        var rows = (IReadOnlyList<CoachDashboardRow>)((OkObjectResult)result).Value!;

        Assert.That(rows[0].ReadinessFlagCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GetDashboard_FlagAgeMaxDays_IsOldestActiveFlag()
    {
        var goal = new GoalEntity { ConsultantUserId = "consultant-lea", CoachUserId = "coach", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 2 };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();

        Db.ReadinessFlags.Add(new ReadinessFlagEntity { GoalId = goal.Id, RaisedAt = DateTime.UtcNow.AddDays(-2) });
        Db.ReadinessFlags.Add(new ReadinessFlagEntity { GoalId = goal.Id, RaisedAt = DateTime.UtcNow.AddDays(-5) });
        await Db.SaveChangesAsync();

        var result = await _sut.GetDashboard();
        var rows = (IReadOnlyList<CoachDashboardRow>)((OkObjectResult)result).Value!;

        Assert.That(rows[0].FlagAgeMaxDays, Is.EqualTo(5));
    }

    [Test]
    public async Task GetDashboard_IsInactive_WhenNoActivityOver21Days()
    {
        var completion = new ResourceCompletionEntity
        {
            ResourceId = (await SeedResourceAsync()),
            UserId = "consultant-lea",
            CompletedAt = DateTime.UtcNow.AddDays(-22),
        };
        Db.ResourceCompletions.Add(completion);
        await Db.SaveChangesAsync();

        var result = await _sut.GetDashboard();
        var rows = (IReadOnlyList<CoachDashboardRow>)((OkObjectResult)result).Value!;

        Assert.That(rows[0].IsInactive, Is.True);
    }

    [Test]
    public async Task GetDashboard_IsNotInactive_WhenActivityWithin21Days()
    {
        var completion = new ResourceCompletionEntity
        {
            ResourceId = (await SeedResourceAsync()),
            UserId = "consultant-lea",
            CompletedAt = DateTime.UtcNow.AddDays(-10),
        };
        Db.ResourceCompletions.Add(completion);
        await Db.SaveChangesAsync();

        var result = await _sut.GetDashboard();
        var rows = (IReadOnlyList<CoachDashboardRow>)((OkObjectResult)result).Value!;

        Assert.That(rows[0].IsInactive, Is.False);
    }

    // ── Activity history ──────────────────────────────────────────────────────

    [Test]
    public async Task GetActivity_WhenConsultantNotFound_ReturnsNotFound()
    {
        var result = await _sut.GetActivity(int.MaxValue);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task GetActivity_ReturnsResourceCompletions()
    {
        var completion = new ResourceCompletionEntity
        {
            ResourceId = (await SeedResourceAsync()),
            UserId = "consultant-lea",
        };
        Db.ResourceCompletions.Add(completion);
        await Db.SaveChangesAsync();

        var result = await _sut.GetActivity(_consultant.Id);

        var ok = (OkObjectResult)result;
        var history = (ConsultantActivityHistory)ok.Value!;
        Assert.That(history.Items.Any(i => i.Type == "resource_completion"), Is.True);
    }

    [Test]
    public async Task GetActivity_ReturnsValidations()
    {
        Db.SkillValidations.Add(new SkillValidationEntity { ConsultantUserId = "consultant-lea", CoachUserId = "coach", SkillId = _skill.Id, Niveau = 2 });
        await Db.SaveChangesAsync();

        var result = await _sut.GetActivity(_consultant.Id);

        var history = (ConsultantActivityHistory)((OkObjectResult)result).Value!;
        Assert.That(history.Items.Any(i => i.Type == "validation"), Is.True);
    }

    [Test]
    public async Task GetActivity_IsSortedNewestFirst()
    {
        var older = new ResourceCompletionEntity { ResourceId = (await SeedResourceAsync()), UserId = "consultant-lea", CompletedAt = DateTime.UtcNow.AddDays(-5) };
        var newer = new SkillValidationEntity { ConsultantUserId = "consultant-lea", CoachUserId = "coach", SkillId = _skill.Id, Niveau = 1 };
        Db.ResourceCompletions.Add(older);
        Db.SkillValidations.Add(newer);
        await Db.SaveChangesAsync();

        var result = await _sut.GetActivity(_consultant.Id);
        var history = (ConsultantActivityHistory)((OkObjectResult)result).Value!;

        Assert.That(history.Items[0].OccurredAt, Is.GreaterThan(history.Items[1].OccurredAt));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<int> SeedResourceAsync()
    {
        var resource = new ResourceEntity { Title = "Test Resource", Url = "https://test.com", Type = ResourceType.Article, SkillId = _skill.Id, FromNiveau = 1, ToNiveau = 2, AddedByUserId = "coach" };
        Db.Resources.Add(resource);
        await Db.SaveChangesAsync();
        return resource.Id;
    }
}
