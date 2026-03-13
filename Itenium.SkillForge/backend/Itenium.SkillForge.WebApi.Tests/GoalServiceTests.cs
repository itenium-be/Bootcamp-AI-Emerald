using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.Entities.Goals;
using Itenium.SkillForge.Entities.Resources;
using Itenium.SkillForge.Entities.Skills;
using Itenium.SkillForge.Services.Goals;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Integration tests for <see cref="GoalService"/>.
/// Issues #18 (Goal API) and #30 (Resource-Goal Linking).
/// </summary>
[TestFixture]
public class GoalServiceTests : DatabaseTestBase
{
    private GoalService _sut = null!;

    private const string CoachUserId = "coach-001";
    private const string ConsultantUserId = "consultant-001";

    [SetUp]
    public async Task SetUp()
    {
        await SkillCatalogueSeedData.Seed(Db);
        _sut = new GoalService(Db);
    }

    // ── GetGoalsForConsultantAsync ────────────────────────────────────────────

    [Test]
    public async Task GetGoalsForConsultant_WhenConsultantNotFound_ReturnsEmptyList()
    {
        var result = await _sut.GetGoalsForConsultantAsync(int.MaxValue);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetGoalsForConsultant_WhenConsultantHasNoGoals_ReturnsEmptyList()
    {
        var consultant = await CreateConsultantAsync();

        var result = await _sut.GetGoalsForConsultantAsync(consultant.Id);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetGoalsForConsultant_ReturnsGoalsWithSkillName()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        await CreateGoalAsync(consultant.UserId, skill.Id);

        var result = await _sut.GetGoalsForConsultantAsync(consultant.Id);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].SkillName, Is.EqualTo(skill.Name));
        Assert.That(result[0].ConsultantUserId, Is.EqualTo(ConsultantUserId));
    }

    [Test]
    public async Task GetGoalsForConsultant_OrdersNewestFirst()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();

        var goal1 = await CreateGoalAsync(consultant.UserId, skill.Id);
        goal1.CreatedAt = DateTime.UtcNow.AddDays(-2);
        var goal2 = await CreateGoalAsync(consultant.UserId, skill.Id);
        goal2.CreatedAt = DateTime.UtcNow.AddDays(-1);
        await Db.SaveChangesAsync();

        var result = await _sut.GetGoalsForConsultantAsync(consultant.Id);

        Assert.That(result[0].Id, Is.EqualTo(goal2.Id));
        Assert.That(result[1].Id, Is.EqualTo(goal1.Id));
    }

    // ── GetGoalAsync ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetGoal_WhenNotFound_ReturnsNull()
    {
        var result = await _sut.GetGoalAsync(int.MaxValue);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetGoal_ReturnsGoalWithFields()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        var goal = await CreateGoalAsync(consultant.UserId, skill.Id, targetNiveau: 5, deadline: DateTime.UtcNow.AddDays(30));

        var result = await _sut.GetGoalAsync(goal.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.TargetNiveau, Is.EqualTo(5));
        Assert.That(result.Deadline, Is.Not.Null);
        Assert.That(result.Status, Is.EqualTo(GoalStatus.Active));
        Assert.That(result.Resources, Is.Empty);
        Assert.That(result.ActiveReadinessFlag, Is.Null);
    }

    // ── CreateGoalAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task CreateGoal_WhenConsultantNotFound_ReturnsNull()
    {
        var skill = await GetAnySkillAsync();
        var request = new CreateGoalRequest(skill.Id, 0, 3, null, null);

        var result = await _sut.CreateGoalAsync(int.MaxValue, request, CoachUserId);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CreateGoal_PersistsGoalWithCorrectFields()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        var deadline = DateTime.UtcNow.AddDays(60).ToUniversalTime();
        var request = new CreateGoalRequest(skill.Id, 2, 5, deadline, null);

        var result = await _sut.CreateGoalAsync(consultant.Id, request, CoachUserId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.SkillId, Is.EqualTo(skill.Id));
        Assert.That(result.CurrentNiveau, Is.EqualTo(2));
        Assert.That(result.TargetNiveau, Is.EqualTo(5));
        Assert.That(result.Deadline, Is.Not.Null);
        Assert.That(result.CoachUserId, Is.EqualTo(CoachUserId));
        Assert.That(result.ConsultantUserId, Is.EqualTo(ConsultantUserId));

        var inDb = await Db.Goals.FindAsync(result.Id);
        Assert.That(inDb, Is.Not.Null);
    }

    [Test]
    public async Task CreateGoal_WithResourceIds_LinksResources()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        var resource = await CreateResourceAsync(skill.Id);
        var request = new CreateGoalRequest(skill.Id, 0, 3, null, [resource.Id]);

        var result = await _sut.CreateGoalAsync(consultant.Id, request, CoachUserId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Resources, Has.Count.EqualTo(1));
        Assert.That(result.Resources[0].ResourceId, Is.EqualTo(resource.Id));
        Assert.That(result.Resources[0].Title, Is.EqualTo(resource.Title));
    }

    // ── UpdateGoalAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task UpdateGoal_WhenNotFound_ReturnsNull()
    {
        var request = new UpdateGoalRequest(1, 3, null, GoalStatus.Active);

        var result = await _sut.UpdateGoalAsync(int.MaxValue, request);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateGoal_UpdatesFields()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        var goal = await CreateGoalAsync(consultant.UserId, skill.Id);
        var newDeadline = DateTime.UtcNow.AddDays(90).ToUniversalTime();
        var request = new UpdateGoalRequest(3, 7, newDeadline, GoalStatus.Active);

        var result = await _sut.UpdateGoalAsync(goal.Id, request);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.CurrentNiveau, Is.EqualTo(3));
        Assert.That(result.TargetNiveau, Is.EqualTo(7));
        Assert.That(result.Status, Is.EqualTo(GoalStatus.Active));
    }

    [Test]
    public async Task UpdateGoal_WithNullDeadline_ClearsDeadline()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        var goal = await CreateGoalAsync(consultant.UserId, skill.Id, deadline: DateTime.UtcNow.AddDays(30));
        var request = new UpdateGoalRequest(goal.CurrentNiveau, goal.TargetNiveau, null, GoalStatus.Active);

        var result = await _sut.UpdateGoalAsync(goal.Id, request);

        Assert.That(result!.Deadline, Is.Null);
    }

    [Test]
    public async Task UpdateGoal_CanMarkAsAchieved()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        var goal = await CreateGoalAsync(consultant.UserId, skill.Id);
        var request = new UpdateGoalRequest(goal.CurrentNiveau, goal.TargetNiveau, null, GoalStatus.Achieved);

        var result = await _sut.UpdateGoalAsync(goal.Id, request);

        Assert.That(result!.Status, Is.EqualTo(GoalStatus.Achieved));
    }

    // ── AddResourceToGoalAsync ───────────────────────────────────────────────

    [Test]
    public async Task AddResourceToGoal_WhenGoalNotFound_ReturnsFalse()
    {
        var skill = await GetAnySkillAsync();
        var resource = await CreateResourceAsync(skill.Id);

        var result = await _sut.AddResourceToGoalAsync(int.MaxValue, resource.Id);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task AddResourceToGoal_LinksResource()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        var goal = await CreateGoalAsync(consultant.UserId, skill.Id);
        var resource = await CreateResourceAsync(skill.Id);

        var result = await _sut.AddResourceToGoalAsync(goal.Id, resource.Id);

        Assert.That(result, Is.True);
        var linked = await Db.GoalResources.AnyAsync(gr => gr.GoalId == goal.Id && gr.ResourceId == resource.Id);
        Assert.That(linked, Is.True);
    }

    [Test]
    public async Task AddResourceToGoal_WhenAlreadyLinked_ReturnsTrue()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        var goal = await CreateGoalAsync(consultant.UserId, skill.Id);
        var resource = await CreateResourceAsync(skill.Id);
        await _sut.AddResourceToGoalAsync(goal.Id, resource.Id);

        var result = await _sut.AddResourceToGoalAsync(goal.Id, resource.Id);

        Assert.That(result, Is.True);
    }

    // ── RemoveResourceFromGoalAsync ──────────────────────────────────────────

    [Test]
    public async Task RemoveResourceFromGoal_WhenNotLinked_ReturnsFalse()
    {
        var skill = await GetAnySkillAsync();
        var consultant = await CreateConsultantAsync();
        var goal = await CreateGoalAsync(consultant.UserId, skill.Id);
        var resource = await CreateResourceAsync(skill.Id);

        var result = await _sut.RemoveResourceFromGoalAsync(goal.Id, resource.Id);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task RemoveResourceFromGoal_RemovesLink()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        var goal = await CreateGoalAsync(consultant.UserId, skill.Id);
        var resource = await CreateResourceAsync(skill.Id);
        await _sut.AddResourceToGoalAsync(goal.Id, resource.Id);

        var result = await _sut.RemoveResourceFromGoalAsync(goal.Id, resource.Id);

        Assert.That(result, Is.True);
        var linked = await Db.GoalResources.AnyAsync(gr => gr.GoalId == goal.Id && gr.ResourceId == resource.Id);
        Assert.That(linked, Is.False);
    }

    // ── Completion status in goal resources ──────────────────────────────────

    [Test]
    public async Task GetGoal_MarksResourceAsCompleted_WhenConsultantCompletedIt()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        var resource = await CreateResourceAsync(skill.Id);
        var goal = await CreateGoalAsync(consultant.UserId, skill.Id);
        Db.GoalResources.Add(new GoalResourceEntity { GoalId = goal.Id, ResourceId = resource.Id });
        Db.ResourceCompletions.Add(new ResourceCompletionEntity
        {
            ResourceId = resource.Id,
            UserId = ConsultantUserId,
        });
        await Db.SaveChangesAsync();

        var result = await _sut.GetGoalAsync(goal.Id);

        Assert.That(result!.Resources[0].IsCompleted, Is.True);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<ConsultantEntity> CreateConsultantAsync()
    {
        var consultant = new ConsultantEntity { UserId = ConsultantUserId, TeamId = 1 };
        Db.Consultants.Add(consultant);
        await Db.SaveChangesAsync();
        return consultant;
    }

    private async Task<SkillEntity> GetAnySkillAsync()
        => await Db.Skills.FirstAsync();

    private async Task<GoalEntity> CreateGoalAsync(
        string consultantUserId,
        int skillId,
        int currentNiveau = 0,
        int targetNiveau = 3,
        DateTime? deadline = null)
    {
        var goal = new GoalEntity
        {
            ConsultantUserId = consultantUserId,
            CoachUserId = CoachUserId,
            SkillId = skillId,
            CurrentNiveau = currentNiveau,
            TargetNiveau = targetNiveau,
            Deadline = deadline,
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
}
