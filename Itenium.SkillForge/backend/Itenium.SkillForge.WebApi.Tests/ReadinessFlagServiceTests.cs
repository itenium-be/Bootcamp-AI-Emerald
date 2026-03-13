using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.Entities.Goals;
using Itenium.SkillForge.Entities.Skills;
using Itenium.SkillForge.Services.Coaching;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Integration tests for <see cref="ReadinessFlagService"/>.
/// Issue #20 (Readiness Flag API).
/// </summary>
[TestFixture]
public class ReadinessFlagServiceTests : DatabaseTestBase
{
    private ReadinessFlagService _sut = null!;

    private const string ConsultantUserId = "consultant-flag-001";
    private const string CoachUserId = "coach-flag-001";

    [SetUp]
    public async Task SetUp()
    {
        await SkillCatalogueSeedData.Seed(Db);
        _sut = new ReadinessFlagService(Db);
    }

    // ── RaiseFlagAsync ────────────────────────────────────────────────────────

    [Test]
    public async Task RaiseFlag_WhenGoalNotFound_ReturnsGoalNotFound()
    {
        var result = await _sut.RaiseFlagAsync(int.MaxValue, ConsultantUserId);

        Assert.That(result, Is.EqualTo(RaiseFlagResult.GoalNotFound));
    }

    [Test]
    public async Task RaiseFlag_WhenNotOwner_ReturnsNotOwner()
    {
        var skill = await GetAnySkillAsync();
        var goal = await CreateGoalAsync(skill.Id, consultantUserId: "other-user");

        var result = await _sut.RaiseFlagAsync(goal.Id, ConsultantUserId);

        Assert.That(result, Is.EqualTo(RaiseFlagResult.NotOwner));
    }

    [Test]
    public async Task RaiseFlag_CreatesFlag_ReturnsSuccess()
    {
        var skill = await GetAnySkillAsync();
        var goal = await CreateGoalAsync(skill.Id);

        var result = await _sut.RaiseFlagAsync(goal.Id, ConsultantUserId);

        Assert.That(result, Is.EqualTo(RaiseFlagResult.Success));

        var flag = await Db.ReadinessFlags.FirstOrDefaultAsync(f => f.GoalId == goal.Id);
        Assert.That(flag, Is.Not.Null);
        Assert.That(flag!.DismissedAt, Is.Null);
    }

    [Test]
    public async Task RaiseFlag_WhenAlreadyActive_ReturnsAlreadyActive()
    {
        var skill = await GetAnySkillAsync();
        var goal = await CreateGoalAsync(skill.Id);
        await _sut.RaiseFlagAsync(goal.Id, ConsultantUserId);

        var result = await _sut.RaiseFlagAsync(goal.Id, ConsultantUserId);

        Assert.That(result, Is.EqualTo(RaiseFlagResult.AlreadyActive));
    }

    [Test]
    public async Task RaiseFlag_AfterDismissal_ReactivatesFlag()
    {
        var skill = await GetAnySkillAsync();
        var goal = await CreateGoalAsync(skill.Id);
        await _sut.RaiseFlagAsync(goal.Id, ConsultantUserId);
        await _sut.DismissFlagAsync(goal.Id);

        var result = await _sut.RaiseFlagAsync(goal.Id, ConsultantUserId);

        Assert.That(result, Is.EqualTo(RaiseFlagResult.Success));
        var flag = await Db.ReadinessFlags.FirstAsync(f => f.GoalId == goal.Id);
        Assert.That(flag.DismissedAt, Is.Null);
    }

    // ── DismissFlagAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task DismissFlag_WhenNoActiveFlag_ReturnsFalse()
    {
        var skill = await GetAnySkillAsync();
        var goal = await CreateGoalAsync(skill.Id);

        var result = await _sut.DismissFlagAsync(goal.Id);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DismissFlag_SetsDismissedAt()
    {
        var skill = await GetAnySkillAsync();
        var goal = await CreateGoalAsync(skill.Id);
        await _sut.RaiseFlagAsync(goal.Id, ConsultantUserId);

        var result = await _sut.DismissFlagAsync(goal.Id);

        Assert.That(result, Is.True);
        var flag = await Db.ReadinessFlags.FirstAsync(f => f.GoalId == goal.Id);
        Assert.That(flag.DismissedAt, Is.Not.Null);
    }

    // ── GetActiveFlagsForConsultantAsync ─────────────────────────────────────

    [Test]
    public async Task GetActiveFlags_WhenConsultantNotFound_ReturnsEmptyList()
    {
        var result = await _sut.GetActiveFlagsForConsultantAsync(int.MaxValue);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetActiveFlags_ReturnsOnlyActiveFlagsForConsultant()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();

        var goalWithFlag = await CreateGoalAsync(skill.Id, consultantUserId: consultant.UserId);
        var goalWithDismissed = await CreateGoalAsync(skill.Id, consultantUserId: consultant.UserId);
        var goalNoFlag = await CreateGoalAsync(skill.Id, consultantUserId: consultant.UserId);

        await _sut.RaiseFlagAsync(goalWithFlag.Id, consultant.UserId);
        await _sut.RaiseFlagAsync(goalWithDismissed.Id, consultant.UserId);
        await _sut.DismissFlagAsync(goalWithDismissed.Id);

        var result = await _sut.GetActiveFlagsForConsultantAsync(consultant.Id);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].GoalId, Is.EqualTo(goalWithFlag.Id));
    }

    [Test]
    public async Task GetActiveFlags_IncludesAgeInDays()
    {
        var consultant = await CreateConsultantAsync();
        var skill = await GetAnySkillAsync();
        var goal = await CreateGoalAsync(skill.Id, consultantUserId: consultant.UserId);
        await _sut.RaiseFlagAsync(goal.Id, consultant.UserId);

        // Backdate the flag
        var flag = await Db.ReadinessFlags.FirstAsync(f => f.GoalId == goal.Id);
        flag.RaisedAt = DateTime.UtcNow.AddDays(-3);
        await Db.SaveChangesAsync();

        var result = await _sut.GetActiveFlagsForConsultantAsync(consultant.Id);

        Assert.That(result[0].AgeDays, Is.GreaterThanOrEqualTo(3));
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

    private async Task<GoalEntity> CreateGoalAsync(int skillId, string? consultantUserId = null)
    {
        var goal = new GoalEntity
        {
            ConsultantUserId = consultantUserId ?? ConsultantUserId,
            CoachUserId = CoachUserId,
            SkillId = skillId,
            CurrentNiveau = 0,
            TargetNiveau = 3,
        };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();
        return goal;
    }
}
