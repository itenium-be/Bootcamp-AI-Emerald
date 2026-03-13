using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.Entities.Goals;
using Itenium.SkillForge.Entities.Resources;
using Itenium.SkillForge.Entities.Skills;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.Services.Activity;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Integration tests for <see cref="ActivityService"/>.
/// Issues #54 (consultant activity history) and #57 (team members list).
/// </summary>
[TestFixture]
public class ActivityServiceTests : DatabaseTestBase
{
    private ActivityService _sut = null!;

    private const string ConsultantUserId = "c-activity-001";
    private const string CoachUserId = "coach-activity-001";
    private const int TeamId = 42;

    [SetUp]
    public async Task SetUp()
    {
        await SkillCatalogueSeedData.Seed(Db);
        _sut = new ActivityService(Db);
    }

    // ── GetActivityAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task GetActivity_WhenConsultantNotFound_ReturnsEmpty()
    {
        var result = await _sut.GetActivityAsync(int.MaxValue);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetActivity_WhenNoEvents_ReturnsEmpty()
    {
        var consultant = await CreateConsultantAsync();

        var result = await _sut.GetActivityAsync(consultant.Id);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetActivity_ReturnsSkillValidations()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        await CreateValidationAsync(ConsultantUserId, skill.Id, 3);

        var result = await _sut.GetActivityAsync(consultant.Id);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].EventType, Is.EqualTo(ActivityEventType.SkillValidated));
        Assert.That(result[0].SkillName, Is.EqualTo(skill.Name));
        Assert.That(result[0].Niveau, Is.EqualTo(3));
    }

    [Test]
    public async Task GetActivity_ReturnsAchievedGoals()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        var goal = await CreateGoalAsync(ConsultantUserId, skill.Id);
        goal.Status = GoalStatus.Achieved;
        goal.AchievedAt = DateTime.UtcNow;
        await Db.SaveChangesAsync();

        var result = await _sut.GetActivityAsync(consultant.Id);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].EventType, Is.EqualTo(ActivityEventType.GoalAchieved));
        Assert.That(result[0].SkillName, Is.EqualTo(skill.Name));
    }

    [Test]
    public async Task GetActivity_DoesNotReturnActiveGoals()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        await CreateGoalAsync(ConsultantUserId, skill.Id);

        var result = await _sut.GetActivityAsync(consultant.Id);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetActivity_ReturnsResourceCompletions()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        var resource = await CreateResourceAsync(skill.Id);
        await CreateCompletionAsync(ConsultantUserId, resource.Id);

        var result = await _sut.GetActivityAsync(consultant.Id);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].EventType, Is.EqualTo(ActivityEventType.ResourceCompleted));
        Assert.That(result[0].ResourceTitle, Is.EqualTo(resource.Title));
    }

    [Test]
    public async Task GetActivity_OrdersNewestFirst()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        var resource = await CreateResourceAsync(skill.Id);

        var old = await CreateCompletionAsync(ConsultantUserId, resource.Id);
        old.CompletedAt = DateTime.UtcNow.AddDays(-5);
        await Db.SaveChangesAsync();

        var recent = new SkillValidationEntity
        {
            ConsultantUserId = ConsultantUserId,
            CoachUserId = CoachUserId,
            SkillId = skill.Id,
            Niveau = 2,
            ValidatedAt = DateTime.UtcNow.AddDays(-1),
        };
        Db.SkillValidations.Add(recent);
        await Db.SaveChangesAsync();

        var result = await _sut.GetActivityAsync(consultant.Id);

        Assert.That(result[0].EventType, Is.EqualTo(ActivityEventType.SkillValidated));
        Assert.That(result[1].EventType, Is.EqualTo(ActivityEventType.ResourceCompleted));
    }

    [Test]
    public async Task GetActivity_DoesNotReturnOtherConsultantsEvents()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        await CreateValidationAsync("other-user", skill.Id, 2);

        var result = await _sut.GetActivityAsync(consultant.Id);

        Assert.That(result, Is.Empty);
    }

    // ── GetTeamMembersAsync ───────────────────────────────────────────────────

    [Test]
    public async Task GetTeamMembers_ReturnsConsultantsInScope()
    {
        var consultant = await CreateConsultantAsync();
        var scope = new TestTeamScope(teamIds: [TeamId]);

        var result = await _sut.GetTeamMembersAsync(scope);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(consultant.Id));
        Assert.That(result[0].UserId, Is.EqualTo(ConsultantUserId));
    }

    [Test]
    public async Task GetTeamMembers_ExcludesConsultantsOutsideScope()
    {
        await CreateConsultantAsync(); // team 42
        var scope = new TestTeamScope(teamIds: [99]); // different team

        var result = await _sut.GetTeamMembersAsync(scope);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetTeamMembers_BackofficeSeesAllTeams()
    {
        await CreateConsultantAsync();
        var scope = new TestTeamScope(isBackOffice: true);

        var result = await _sut.GetTeamMembersAsync(scope);

        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public async Task GetTeamMembers_CountsActiveGoals()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        await CreateGoalAsync(ConsultantUserId, skill.Id); // Active
        var achieved = await CreateGoalAsync(ConsultantUserId, skill.Id);
        achieved.Status = GoalStatus.Achieved;
        await Db.SaveChangesAsync();
        var scope = new TestTeamScope(teamIds: [TeamId]);

        var result = await _sut.GetTeamMembersAsync(scope);

        Assert.That(result[0].ActiveGoalCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GetTeamMembers_CountsActiveFlags()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        var goal = await CreateGoalAsync(ConsultantUserId, skill.Id);
        Db.ReadinessFlags.Add(new ReadinessFlagEntity { GoalId = goal.Id });
        await Db.SaveChangesAsync();
        var scope = new TestTeamScope(teamIds: [TeamId]);

        var result = await _sut.GetTeamMembersAsync(scope);

        Assert.That(result[0].ActiveFlagCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GetTeamMembers_ExcludesArchivedConsultants()
    {
        var consultant = await CreateConsultantAsync();
        consultant.ArchivedAt = DateTime.UtcNow;
        await Db.SaveChangesAsync();
        var scope = new TestTeamScope(teamIds: [TeamId]);

        var result = await _sut.GetTeamMembersAsync(scope);

        Assert.That(result, Is.Empty);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<ConsultantEntity> CreateConsultantAsync()
    {
        var consultant = new ConsultantEntity { UserId = ConsultantUserId, TeamId = TeamId };
        Db.Consultants.Add(consultant);
        await Db.SaveChangesAsync();
        return consultant;
    }

    private async Task<SkillEntity> GetAnySkillAsync()
        => await Db.Skills.FirstAsync();

    private async Task<GoalEntity> CreateGoalAsync(string consultantUserId, int skillId)
    {
        var goal = new GoalEntity
        {
            ConsultantUserId = consultantUserId,
            CoachUserId = CoachUserId,
            SkillId = skillId,
            CurrentNiveau = 0,
            TargetNiveau = 3,
        };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();
        return goal;
    }

    private async Task<ResourceEntity> CreateResourceAsync(int skillId)
    {
        var resource = new ResourceEntity
        {
            Title = "Test Resource",
            Url = "https://example.com",
            Type = ResourceType.Article,
            SkillId = skillId,
            FromNiveau = 1,
            ToNiveau = 3,
            AddedByUserId = CoachUserId,
        };
        Db.Resources.Add(resource);
        await Db.SaveChangesAsync();
        return resource;
    }

    private async Task<SkillValidationEntity> CreateValidationAsync(string userId, int skillId, int niveau)
    {
        var validation = new SkillValidationEntity
        {
            ConsultantUserId = userId,
            CoachUserId = CoachUserId,
            SkillId = skillId,
            Niveau = niveau,
        };
        Db.SkillValidations.Add(validation);
        await Db.SaveChangesAsync();
        return validation;
    }

    private async Task<ResourceCompletionEntity> CreateCompletionAsync(string userId, int resourceId)
    {
        var completion = new ResourceCompletionEntity
        {
            ResourceId = resourceId,
            UserId = userId,
        };
        Db.ResourceCompletions.Add(completion);
        await Db.SaveChangesAsync();
        return completion;
    }
}

/// <summary>Test stub for ITeamQueryScope.</summary>
internal sealed class TestTeamScope : ITeamQueryScope
{
    private readonly bool _isBackOffice;
    private readonly ICollection<int> _teamIds;

    public TestTeamScope(bool isBackOffice = false, ICollection<int>? teamIds = null)
    {
        _isBackOffice = isBackOffice;
        _teamIds = teamIds ?? [];
    }

    public bool IsBackOffice => _isBackOffice;
    public ICollection<int> TeamIds => _teamIds;
    public bool CanAccessTeam(int teamId) => _isBackOffice || _teamIds.Contains(teamId);
}
